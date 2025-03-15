using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Managers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using ClientUtils = Jellyfin.Plugin.ListenBrainz.Clients.Utils;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Scheduled Jellyfin task for syncing loved tracks from ListenBrainz to Jellyfin.
/// </summary>
public class LovedTracksSyncTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly IMusicBrainzClient _musicBrainzClient;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataRepository _repository;
    private readonly IUserDataManager _userDataManager;
    private readonly IPluginConfigManager _configManager;
    private bool _reenableImmediateSync;
    private double _progress;
    private double _userCountRatio;

    /// <summary>
    /// Initializes a new instance of the <see cref="LovedTracksSyncTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="dataRepository">Data repository.</param>
    /// <param name="dataManager">User data manager.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="musicBrainzClient">MusicBrainz client.</param>
    /// <param name="pluginConfig">Plugin configuration manager.</param>
    public LovedTracksSyncTask(
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataRepository dataRepository,
        IUserDataManager dataManager,
        IListenBrainzClient? listenBrainzClient = null,
        IMusicBrainzClient? musicBrainzClient = null,
        IPluginConfigManager? pluginConfig = null)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.LovedSyncTask");
        _listenBrainzClient = listenBrainzClient ??
                              ClientUtils.GetListenBrainzClient(_logger, clientFactory, libraryManager);
        _musicBrainzClient = musicBrainzClient ?? ClientUtils.GetMusicBrainzClient(_logger, clientFactory);
        _libraryManager = libraryManager;
        _userManager = userManager;
        _repository = dataRepository;
        _userDataManager = dataManager;
        _configManager = pluginConfig ?? new PluginConfigManager();
    }

    /// <inheritdoc />
    public string Name => "Synchronize loved tracks";

    /// <inheritdoc />
    public string Key => "SyncLovedTracks";

    /// <inheritdoc />
    public string Description => "Synchronize loved tracks from ListenBrainz to Jellyfin";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        using var logScope = BeginLogScope();
        var conf = _configManager.GetConfiguration();

        if (!conf.IsMusicBrainzEnabled)
        {
            _logger.LogInformation("MusicBrainz integration is disabled, cannot sync favorites");
            return;
        }

        if (conf.IsImmediateFavoriteSyncEnabled)
        {
            _logger.LogInformation("Immediate favorite sync is enabled, disabling it temporarily");
            _reenableImmediateSync = true;
            SetImmediateFavSyncEnabled(false);
        }

        try
        {
            _logger.LogInformation("Starting favorite sync from ListenBrainz...");
            ResetProgress(conf.UserConfigs.Count);
            foreach (var userConfig in conf.UserConfigs)
            {
                _logger.LogInformation("Syncing favorites for user {Username}", userConfig.UserName);
                if (!userConfig.IsFavoritesSyncEnabled)
                {
                    _logger.LogInformation("User has not favorite syncing enabled, skipping");
                    _progress += _userCountRatio;
                    progress.Report(_progress);
                    continue;
                }

                await HandleFavoriteSync(progress, userConfig, cancellationToken);
            }
        }
        finally
        {
            if (_reenableImmediateSync)
            {
                _logger.LogInformation("Re-enabling Immediate favorite sync");
                SetImmediateFavSyncEnabled(true);
            }
        }
    }

    private void SetImmediateFavSyncEnabled(bool isEnabled)
    {
        var conf = _configManager.GetConfiguration();
        conf.IsImmediateFavoriteSyncEnabled = isEnabled;
        _configManager.SaveConfiguration(conf);
    }

    private async Task HandleFavoriteSync(
        IProgress<double> progress,
        UserConfig userConfig,
        CancellationToken cancellationToken)
    {
        var lovedTracksIds = (await _listenBrainzClient.GetLovedTracksAsync(userConfig, cancellationToken)).ToList();
        var user = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (user is null)
        {
            _logger.LogError("User with ID {UserId} does not exist", userConfig.JellyfinUserId);
            return;
        }

        var allowedLibraries = GetAllowedLibraries().Select(al => _libraryManager.GetItemById(al)).WhereNotNull();
        var q = new InternalItemsQuery(user)
        {
            // Future-proofing for music videos
            MediaTypes = new[] { MediaType.Audio, MediaType.Video }
        };

        var items = _libraryManager
            .GetItemList(q, allowedLibraries.ToList())
            .Where(i => !_userDataManager.GetUserData(user, i).IsFavorite)
            .Where(i => i.ProviderIds.GetValueOrDefault("MusicBrainzTrack") is not null)
            .ToList();

        foreach (var item in items)
        {
            var recordingMbid = string.Empty;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                recordingMbid = _musicBrainzClient.GetAudioItemMetadata(item).RecordingMbid;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Task has been cancelled");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to get metadata for item {ItemId}: {Error}", item.Id, e.Message);
            }

            if (lovedTracksIds.Contains(recordingMbid))
            {
                MarkAsFavorite(user, item, cancellationToken);
            }

            _progress += _userCountRatio / items.Count;
            progress.Report(_progress);
        }
    }

    private IEnumerable<Guid> GetAllowedLibraries()
    {
        var allLibraries = _configManager.GetConfiguration().LibraryConfigs;
        if (allLibraries.Count > 0)
        {
            return allLibraries.Where(lc => lc.IsAllowed).Select(lc => lc.Id);
        }

        return _libraryManager.GetMusicLibraries().Select(ml => ml.Id);
    }

    /// <summary>
    /// Update favorite status of a <see cref="BaseItem"/> without invoking an event.
    /// </summary>
    /// <param name="user">User associated with the change.</param>
    /// <param name="item">Affected item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private void MarkAsFavorite(User user, BaseItem item, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Marking item {Name} as favorite for user {User}", item.Name, user.Username);
        var userData = _userDataManager.GetUserData(user, item);
        userData.IsFavorite = true;

        if (_configManager.GetConfiguration().ShouldEmitUserRatingEvent)
        {
            // This spams UpdateUserRating events, which feeds into Immediate favorite sync feature.
            // But there might be other plugins reacting on this event, so if the plugin should produce these events
            // the plugin temporarily disables the immediate sync feature (see usages of SetImmediateFavSyncEnabled()).
            _userDataManager.SaveUserData(user, item, userData, UserDataSaveReason.UpdateUserRating, cancellationToken);
            return;
        }

        foreach (var key in item.GetUserDataKeys())
        {
            _repository.SaveUserData(user.InternalId, key, userData, cancellationToken);
        }

        _logger.LogDebug("Item {Name} has been marked as favorite for user {User}", item.Name, user.Username);
    }

    private void ResetProgress(int userCount)
    {
        _userCountRatio = 100.0 / userCount;
        _progress = 0;
    }

    private IDisposable? BeginLogScope()
    {
        return _logger.BeginScope(new Dictionary<string, object> { { "EventId", "LovedTracksSyncTask" } });
    }
}
