using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of the IFavoriteSyncService.
/// </summary>
public class DefaultFavoriteSyncService : IFavoriteSyncService
{
    private readonly ILogger _logger;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly IMusicBrainzClient _musicBrainzClient;
    private readonly IPluginConfigService _pluginConfigService;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;
    private bool _isEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFavoriteSyncService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="musicBrainzClient">MusicBrainz client.</param>
    /// <param name="pluginConfigService">Plugin config service.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="userDataManager">User data manager.</param>
    public DefaultFavoriteSyncService(
        ILogger logger,
        IListenBrainzClient listenBrainzClient,
        IMusicBrainzClient musicBrainzClient,
        IPluginConfigService pluginConfigService,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager)
    {
        _logger = logger;
        _listenBrainzClient = listenBrainzClient;
        _musicBrainzClient = musicBrainzClient;
        _pluginConfigService = pluginConfigService;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        _isEnabled = true;
        Instance = this;
    }

    /// <summary>
    /// Gets a singleton instance of the favorite sync service.
    /// </summary>
    public static IFavoriteSyncService? Instance { get; private set; }

    /// <inheritdoc />
    public void SyncToListenBrainz(Guid itemId, Guid jellyfinUserId, long? listenTs = null)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Favorite sync service is disabled, cancelling sync");
            return;
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            _logger.LogWarning("Item with ID {ItemId} not found", itemId);
            return;
        }

        var userConfig = _pluginConfigService.GetUserConfig(jellyfinUserId);
        if (userConfig is null)
        {
            _logger.LogWarning("ListenBrainz config for user ID {UserId} not found", jellyfinUserId);
            return;
        }

        var jellyfinUser = _userManager.GetUserById(jellyfinUserId);
        if (jellyfinUser is null)
        {
            _logger.LogWarning("User with ID {UserId} not found", jellyfinUserId);
            return;
        }

        var userItemData = _userDataManager.GetUserData(jellyfinUser, item);

        var recordingMbid = item.ProviderIds.GetValueOrDefault("MusicBrainzRecording");
        if (string.IsNullOrEmpty(recordingMbid) && _pluginConfigService.IsMusicBrainzEnabled)
        {
            try
            {
                var metadata = _musicBrainzClient.GetAudioItemMetadata(item);
                recordingMbid = metadata.RecordingMbid;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e, "Failed to get recording MBID");
            }
        }

        if (string.IsNullOrEmpty(recordingMbid) && listenTs is null)
        {
            _logger.LogInformation("No recording MBID is available, cannot sync favorite");
            return;
        }

        try
        {
            _logger.LogInformation("Attempting to sync favorite status");
            _listenBrainzClient.SendFeedback(userConfig, userItemData.IsFavorite, recordingMbid);
            _logger.LogInformation("Favorite sync has been successful");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Favorite sync failed: {Reason}", e.Message);
            _logger.LogDebug(e, "Favorite sync failed");
        }
    }

    /// <inheritdoc />
    public void Enable()
    {
        _isEnabled = true;
        _logger.LogDebug("Favorite sync service has been enabled");
    }

    /// <inheritdoc />
    public void Disable()
    {
        _isEnabled = false;
        _logger.LogDebug("Favorite sync service has been disabled");
    }
}
