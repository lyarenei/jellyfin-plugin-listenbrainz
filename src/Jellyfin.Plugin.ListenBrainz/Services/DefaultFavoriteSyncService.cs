using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
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
    private readonly IMetadataProviderService _metadataProvider;
    private readonly IPluginConfigService _pluginConfigService;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IUserDataManager _userDataManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFavoriteSyncService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="musicBrainzClient">MusicBrainz client.</param>
    /// <param name="metadataProvider">Metadata provider.</param>
    /// <param name="pluginConfigService">Plugin config service.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="userDataManager">User data manager.</param>
    public DefaultFavoriteSyncService(
        ILogger logger,
        IListenBrainzClient listenBrainzClient,
        IMusicBrainzClient musicBrainzClient,
        IMetadataProviderService metadataProvider,
        IPluginConfigService pluginConfigService,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager)
    {
        _logger = logger;
        _listenBrainzClient = listenBrainzClient;
        _musicBrainzClient = musicBrainzClient;
        _metadataProvider = metadataProvider;
        _pluginConfigService = pluginConfigService;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _userDataManager = userDataManager;
        IsEnabled = true;
        Instance = this;
    }

    /// <inheritdoc />
    public bool IsEnabled { get; private set; }

    /// <inheritdoc />
    public bool IsDisabled => !IsEnabled;

    /// <summary>
    /// Gets a singleton instance of the favorite sync service.
    /// </summary>
    public static IFavoriteSyncService? Instance { get; private set; }

    /// <inheritdoc />
    public void SyncToListenBrainz(Guid itemId, Guid jellyfinUserId, long? listenTs = null)
    {
        if (!IsEnabled)
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

        string? recordingMsid = null;
        if (string.IsNullOrEmpty(recordingMbid))
        {
            if (listenTs is null)
            {
                _logger.LogInformation("No recording MBID is available, cannot sync favorite");
                return;
            }

            _logger.LogInformation("Recording MBID not found, trying to get recording MSID for the sync");
            recordingMsid = GetRecordingMsid(userConfig, listenTs.Value);
        }

        try
        {
            _logger.LogInformation("Attempting to sync favorite status");
            _listenBrainzClient.SendFeedback(userConfig, userItemData.IsFavorite, recordingMbid, recordingMsid);
            _logger.LogInformation("Favorite sync has been successful");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Favorite sync failed: {Reason}", e.Message);
            _logger.LogDebug(e, "Favorite sync failed");
        }
    }

    /// <inheritdoc />
    public async Task<bool> SyncToListenBrainzAsync(
        Guid itemId,
        Guid jellyfinUserId,
        long? listenTs,
        CancellationToken cancellationToken)
    {
        if (!IsEnabled)
        {
            _logger.LogDebug("Favorite sync service is disabled, nothing to do");
            return false;
        }

        var item = _libraryManager.GetItemById(itemId);
        if (item is null)
        {
            _logger.LogWarning("Item with ID {ItemId} not found", itemId);
            return false;
        }

        var userConfig = _pluginConfigService.GetUserConfig(jellyfinUserId);
        if (userConfig is null)
        {
            _logger.LogDebug("ListenBrainz config for user ID {UserId} not found", jellyfinUserId);
            return false;
        }

        var jellyfinUser = _userManager.GetUserById(jellyfinUserId);
        if (jellyfinUser is null)
        {
            _logger.LogWarning("User with ID {UserId} not found", jellyfinUserId);
            return false;
        }

        var userItemData = _userDataManager.GetUserData(jellyfinUser, item);
        if (item is not Audio audioItem)
        {
            _logger.LogDebug("Not supported item type for favorite sync: {ItemType}", item.GetType().Name);
            return false;
        }

        var recordingMbid = item.GetRecordingMbid();
        if (string.IsNullOrEmpty(recordingMbid) && _pluginConfigService.IsMusicBrainzEnabled)
        {
            _logger.LogDebug("Recording MBID is not available, attempting to get it from MusicBrainz");
            try
            {
                var metadata = await _metadataProvider.GetAudioItemMetadataAsync(audioItem, cancellationToken);
                recordingMbid = metadata?.RecordingMbid;
            }
            catch (Exception e)
            {
                _logger.LogDebug("Failed to get recording MBID: {Reason}", e.Message);
            }
        }

        string? recordingMsid = null;
        if (string.IsNullOrEmpty(recordingMbid))
        {
            _logger.LogDebug("Recording MBID is not available, switching to recording MSID method");
            if (listenTs is null)
            {
                _logger.LogDebug("Listen timestamp is not available, cannot get recording MSID");
                return false;
            }

            recordingMsid = await GetRecordingMsidAsync(userConfig, listenTs.Value, cancellationToken);
        }

        _logger.LogDebug("Attempting to sync favorite status");
        await _listenBrainzClient.SendFeedbackAsync(
            userConfig,
            userItemData.IsFavorite,
            recordingMbid,
            recordingMsid,
            cancellationToken);

        _logger.LogInformation("Favorite sync has been successful");
        return true;
    }

    /// <inheritdoc />
    public void Enable()
    {
        IsEnabled = true;
        _logger.LogDebug("Favorite sync service has been enabled");
    }

    /// <inheritdoc />
    public void Disable()
    {
        IsEnabled = false;
        _logger.LogDebug("Favorite sync service has been disabled");
    }

    private string? GetRecordingMsid(UserConfig userConfig, long listenTs)
    {
        const int MaxAttempts = 4;
        const int BackOffSecs = 5;
        var sleepSecs = 1;

        // Delay to maximize the chance of getting it on first try
        Thread.Sleep(500);
        for (int i = 0; i < MaxAttempts; i++)
        {
            _logger.LogDebug("Attempt number {Attempt} to get recording MSID", i + 1);
            try
            {
                _logger.LogInformation("Attempting to get recording MSID");
                return _listenBrainzClient.GetRecordingMsidByListenTs(userConfig, listenTs);
            }
            catch (Exception e)
            {
                _logger.LogInformation("Failed to get recording MSID: {Reason}", e.Message);
                _logger.LogDebug(e, "Failed to get recording MSID");
            }

            sleepSecs *= BackOffSecs;
            sleepSecs += new Random().Next(20);
            _logger.LogDebug(
                "Recording MSID with listen timestamp {Ts} not found, will retry in {Secs} seconds",
                listenTs,
                sleepSecs);

            Thread.Sleep(sleepSecs * 1000);
        }

        return null;
    }

    private async Task<string?> GetRecordingMsidAsync(UserConfig userConfig, long listenTs, CancellationToken cancellationToken)
    {
        const int MaxAttempts = 4;
        const int BackOffSecs = 5;
        var sleepSecs = 1;

        // Delay to maximize the chance of getting it on first try
        await Task.Delay(500, cancellationToken);
        for (int i = 0; i < MaxAttempts; i++)
        {
            _logger.LogDebug("Attempt number {Attempt} to get recording MSID", i + 1);
            try
            {
                _logger.LogDebug("Attempting to get recording MSID");
                return await _listenBrainzClient.GetRecordingMsidByListenTsAsync(userConfig, listenTs, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogDebug("Failed to get recording MSID: {Reason}", e.Message);
                _logger.LogTrace(e, "Failed to get recording MSID");
            }

            sleepSecs *= BackOffSecs;
            sleepSecs += new Random().Next(20);
            _logger.LogDebug(
                "Recording MSID with listen timestamp {Ts} not found, will retry in {Secs} seconds",
                listenTs,
                sleepSecs);

            await Task.Delay(sleepSecs * 1000, cancellationToken);
        }

        return null;
    }
}
