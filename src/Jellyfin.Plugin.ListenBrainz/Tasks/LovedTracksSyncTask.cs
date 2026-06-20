using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
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
    private readonly IListenBrainzService _listenBrainz;
    private readonly IMetadataProviderService _metadataProvider;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private readonly IPluginConfigService _configService;
    private readonly IFavoriteSyncService _favoriteSyncService;
    private double _progress;
    private double _userCountRatio;

    /// <summary>
    /// Initializes a new instance of the <see cref="LovedTracksSyncTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="dataManager">User data manager.</param>
    /// <param name="listenBrainz">ListenBrainz service.</param>
    /// <param name="metadataProvider">Metadata provider service.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="favoriteSyncService">Favorite sync service.</param>
    public LovedTracksSyncTask(
        ILoggerFactory loggerFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager dataManager,
        IListenBrainzService listenBrainz,
        IMetadataProviderService metadataProvider,
        IPluginConfigService configService,
        IFavoriteSyncService favoriteSyncService)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.LovedSyncTask");
        _listenBrainz = listenBrainz;
        _metadataProvider = metadataProvider;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = dataManager;
        _configService = configService;
        _favoriteSyncService = favoriteSyncService;
    }

    /// <inheritdoc />
    public string Name => "Sync loved tracks";

    /// <inheritdoc />
    public string Key => "SyncLovedTracks";

    /// <inheritdoc />
    public string Description => "Get loved tracks from ListenBrainz and mark them as favorite in Jellyfin";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => Array.Empty<TaskTriggerInfo>();

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        using var logScope = BeginLogScope();
        if (_configService.UserConfigs.Count == 0)
        {
            _logger.LogInformation("No users have been configured, nothing to sync");
            progress.Report(100);
            return;
        }

        if (!_configService.IsMusicBrainzEnabled)
        {
            _logger.LogInformation("MusicBrainz integration is disabled, some favorites may not be synced");
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

    private async Task HandleFavoriteSync(
        IProgress<double> progress,
        UserConfig userConfig,
        CancellationToken cancellationToken)
    {
        var lovedTracksIds = (await _listenBrainz.GetLovedTracksAsync(userConfig, cancellationToken)).ToList();
        var user = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (user is null)
        {
            _logger.LogError("User with ID {UserId} does not exist", userConfig.JellyfinUserId);
            return;
        }

        var allowedLibraries = GetAllowedLibraries().Select(al => _libraryManager.GetItemById(al)).WhereNotNull();
        var q = new InternalItemsQuery(user) { MediaTypes = [MediaType.Audio] };

        var items = _libraryManager
            .GetItemList(q, allowedLibraries.ToList())
            .Where(i => !_userDataManager.GetUserData(user, i)?.IsFavorite ?? false)
            .Where(i => i.GetRecordingMbid() is not null || i.GetTrackMbid() is not null)
            .ToList();

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var recordingMbid = string.Empty;

            try
            {
                recordingMbid = item.GetRecordingMbid();
                if (string.IsNullOrEmpty(recordingMbid) && _configService.IsMusicBrainzEnabled && item is Audio audioItem)
                {
                    _logger.LogDebug("Fetching recording MBID for item {ItemId} from MusicBrainz", item.Id);
                    var metadata = await _metadataProvider.GetAudioItemMetadataAsync(audioItem, cancellationToken);
                    recordingMbid = metadata?.RecordingMbid;
                }
                else
                {
                    _logger.LogDebug("Recording MBID for item {ItemId} is not available, skipping", item.Id);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning("Processing item {ItemId} failed: {Error}", item.Id, e.Message);
            }

            if (recordingMbid is not null && lovedTracksIds.Contains(recordingMbid))
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
        if (userData is null)
        {
            _logger.LogInformation(
                "Could not mark item {Name} as favorite for user {User}: no user data available",
                item.Name,
                user.Username);
            return;
        }

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
