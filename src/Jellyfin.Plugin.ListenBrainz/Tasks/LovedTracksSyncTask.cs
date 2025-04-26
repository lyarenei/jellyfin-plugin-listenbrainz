using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
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
    private readonly IPluginConfigService _configService;
    private IFavoriteSyncService? _favoriteSyncService;
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
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="favoriteSyncService">Favorite sync service.</param>
    public LovedTracksSyncTask(
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataRepository dataRepository,
        IUserDataManager dataManager,
        IListenBrainzClient? listenBrainzClient = null,
        IMusicBrainzClient? musicBrainzClient = null,
        IPluginConfigService? configService = null,
        IFavoriteSyncService? favoriteSyncService = null)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.LovedSyncTask");
        _listenBrainzClient = listenBrainzClient ?? ClientUtils.GetListenBrainzClient(_logger, clientFactory);
        _musicBrainzClient = musicBrainzClient ?? ClientUtils.GetMusicBrainzClient(_logger, clientFactory);
        _libraryManager = libraryManager;
        _userManager = userManager;
        _repository = dataRepository;
        _userDataManager = dataManager;
        _configService = configService ?? new DefaultPluginConfigService();
        _favoriteSyncService = favoriteSyncService ?? DefaultFavoriteSyncService.Instance;
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
        if (_favoriteSyncService is null)
        {
            _favoriteSyncService = DefaultFavoriteSyncService.Instance;
            if (_favoriteSyncService is null)
            {
                _logger.LogError("Favorite sync service is not available, cannot sync favorites");
                return;
            }
        }

        if (!_configService.IsMusicBrainzEnabled)
        {
            _logger.LogInformation("MusicBrainz integration is disabled, cannot sync favorites");
            return;
        }

        if (_configService.UserConfigs.Count == 0)
        {
            _logger.LogInformation("No users have been configured, cannot sync");
            progress.Report(100);
            return;
        }

        _logger.LogInformation("Starting favorite sync from ListenBrainz...");
        ResetProgress(_configService.UserConfigs.Count);

        _logger.LogDebug("Temporarily disabling favorite sync service");
        _favoriteSyncService.Disable();

        try
        {
            foreach (var userConfig in _configService.UserConfigs)
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
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Favorite sync task has been cancelled");
            progress.Report(100);
        }
        finally
        {
            _logger.LogDebug("Re-enabling favorite sync service");
            _favoriteSyncService.Enable();
        }
    }

    private void SetImmediateFavSyncEnabled(bool isEnabled)
    {
        var conf = _configService.GetConfiguration();
        conf.IsImmediateFavoriteSyncEnabled = isEnabled;
        _configService.SaveConfiguration(conf);
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
                _logger.LogDebug("Sync task has been cancelled");
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
        var allLibraries = _configService.LibraryConfigs;
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

        _userDataManager.SaveUserData(user, item, userData, UserDataSaveReason.UpdateUserRating, cancellationToken);
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
