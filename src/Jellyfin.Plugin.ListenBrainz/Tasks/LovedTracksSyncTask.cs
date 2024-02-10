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
        if (!Plugin.GetConfiguration().IsMusicBrainzEnabled)
        {
            _logger.LogInformation("MusicBrainz integration is disabled, cannot sync favorites");
            return;
        }

        var conf = Plugin.GetConfiguration();
        foreach (var userConfig in conf.UserConfigs)
        {
            if (!userConfig.IsFavoritesSyncEnabled)
            {
                _logger.LogDebug("User has not favorite syncing enabled, skipping");
                continue;
            }

            await HandleFavoriteSync(userConfig, cancellationToken);
        }
    }

    private async Task HandleFavoriteSync(UserConfig userConfig, CancellationToken cancellationToken)
    {
        var userFeedback = await _listenBrainzClient.GetLovedTracksAsync(userConfig, cancellationToken);
        var user = _userManager.GetUserById(userConfig.JellyfinUserId);
        var userData = _repository.GetAllUserData(user.InternalId);

        var allowedLibraries = GetAllowedLibraries().Select(al => _libraryManager.GetItemById(al));
        var q = new InternalItemsQuery(user);
        var itemsWithRecordingId = _libraryManager
            .GetItemList(q, allowedLibraries.ToList())
            .Where(i => i.ProviderIds.GetValueOrDefault("MusicBrainzTrack") is not null)
            .Select(i => (i, _metadataClient.GetAudioItemMetadata(i).RecordingMbid))
            .Where(i => userFeedback.Contains(i.RecordingMbid));

        // TODO: plugin option
        if (false)
        {
            // This spams UpdateUserRating events, which will feed into Immediate favorite sync feature.
            // But there might be other plugins reacting on this event, so there should be an option to produce them.
            // TODO: internal flag to disable immediate sync during this
            foreach (var tuple in itemsWithRecordingId)
            {
                var data = _userDataManager.GetUserData(user, tuple.i);
                data.IsFavorite = true;
                _userDataManager.SaveUserData(user, tuple.i, data, UserDataSaveReason.UpdateUserRating, cancellationToken);
            }

            return;
        }

        // TODO: solution without user rating event spam

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
    /// Converts <see cref="UserItemData.Key"/> to a GUID string.
    /// </summary>
    /// <param name="key">Key to convert.</param>
    /// <returns>GUID string. Empty on failure.</returns>
    private static string KeyToGuidString(string key)
    {
        try
        {
            return new Guid(key).ToString();
        }
        catch (FormatException)
        {
            return string.Empty;
        }
    }
}
