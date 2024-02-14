using Jellyfin.Data.Entities;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Utils;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Scheduled Jellyfin task for syncing loved tracks from ListenBrainz to Jellyfin.
/// </summary>
public class LovedTracksSyncTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly IMetadataClient _metadataClient;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataRepository _repository;
    private readonly IUserDataManager _userDataManager;
    private readonly int _usersCount;
    private bool _reenableImmediateSync;

    /// <summary>
    /// Initializes a new instance of the <see cref="LovedTracksSyncTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="dataRepository">Data repository.</param>
    /// <param name="dataManager">User data manager.</param>
    public LovedTracksSyncTask(
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataRepository dataRepository,
        IUserDataManager dataManager)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.FavoriteSyncTask");
        _listenBrainzClient = ClientUtils.GetListenBrainzClient(_logger, clientFactory, libraryManager);
        _metadataClient = ClientUtils.GetMusicBrainzClient(_logger, clientFactory);
        _libraryManager = libraryManager;
        _userManager = userManager;
        _repository = dataRepository;
        _userDataManager = dataManager;
        _usersCount = Plugin.GetConfiguration().UserConfigs.Count;
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
        var conf = Plugin.GetConfiguration();
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
            foreach (var userConfig in conf.UserConfigs)
            {
                _logger.LogInformation("Syncing favorites for user {Username}", userConfig.UserName);
                if (!userConfig.IsFavoritesSyncEnabled)
                {
                    _logger.LogInformation("User has not favorite syncing enabled, skipping");
                    progress.Report(100.0 / _usersCount);
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

    private static void SetImmediateFavSyncEnabled(bool isEnabled)
    {
        var conf = Plugin.GetConfiguration();
        if (conf.IsImmediateFavoriteSyncEnabled)
        {
            conf.IsImmediateFavoriteSyncEnabled = isEnabled;
            Plugin.UpdateConfig(conf);
        }
    }

    private async Task HandleFavoriteSync(IProgress<double> progress, UserConfig userConfig, CancellationToken cancellationToken)
    {
        var lovedTracksIds = (await _listenBrainzClient.GetLovedTracksAsync(userConfig, cancellationToken)).ToList();
        var user = _userManager.GetUserById(userConfig.JellyfinUserId);

        var allowedLibraries = GetAllowedLibraries().Select(al => _libraryManager.GetItemById(al));
        var q = new InternalItemsQuery(user)
        {
            // Future-proofing if music videos are supported in the future
            MediaTypes = new[] { MediaType.Audio, MediaType.Video }
        };

        var taggedItems = _libraryManager
            .GetItemList(q, allowedLibraries.ToList())
            .Where(i => !_userDataManager.GetUserData(userConfig.JellyfinUserId, i).IsFavorite)
            .Where(i => i.ProviderIds.GetValueOrDefault("MusicBrainzTrack") is not null)
            .Select(i => (Item: i, _metadataClient.GetAudioItemMetadata(i).RecordingMbid));

        var multiplier = 1;
        foreach (var item in taggedItems)
        {
            if (lovedTracksIds.Contains(item.RecordingMbid))
            {
                MarkAsFavorite(user, item.Item, cancellationToken);
                multiplier = 1;
            }

            progress.Report(100.0 / _usersCount / (lovedTracksIds.Count * multiplier));
            multiplier++;
        }
    }

    private IEnumerable<Guid> GetAllowedLibraries()
    {
        var allLibraries = Plugin.GetConfiguration().LibraryConfigs;
        if (allLibraries.Any())
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
        _logger.LogDebug("Marking item {Name} ({Id}) as favorite", item.Name, item.Id);
        var userData = _userDataManager.GetUserData(user, item);
        userData.IsFavorite = true;

        if (Plugin.GetConfiguration().ShouldEmitUserRatingEvent)
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
    }
}
