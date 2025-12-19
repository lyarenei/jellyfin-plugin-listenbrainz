using Jellyfin.Plugin.ListenBrainz.Common;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Managers;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Handlers;

/// <summary>
/// Handler for <see cref="IUserDataManager.UserDataSaved"/> events.
/// </summary>
public class UserDataSaveHandler : GenericHandler<UserDataSaveEventArgs>
{
    private readonly ILogger _logger;
    private readonly IPluginConfigService _configService;
    private readonly IFavoriteSyncService _favoriteSyncService;
    private readonly IValidationService _validationService;
    private readonly IMusicBrainzClient _musicBrainzClient;
    private readonly IBackupManager _backupManager;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly ListensCacheManager _listensCache;
    private readonly PlaybackTrackingManager _playbackTracker;

    private readonly object _userDataSaveLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataSaveHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="configService">Plugin config service.</param>
    /// <param name="favoriteSyncService">Favorite sync service.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="musicBrainzClient">MusicBrainz client.</param>
    /// <param name="backupManager">Backup manager.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="listensCache">Listen cache manager.</param>
    /// <param name="playbackTracker">Playback tracker.</param>
    public UserDataSaveHandler(
        ILogger logger,
        IUserManager userManager,
        IPluginConfigService configService,
        IFavoriteSyncService favoriteSyncService,
        IValidationService validationService,
        IMusicBrainzClient musicBrainzClient,
        IBackupManager backupManager,
        IListenBrainzClient listenBrainzClient,
        ListensCacheManager listensCache,
        PlaybackTrackingManager playbackTracker) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _favoriteSyncService = favoriteSyncService;
        _validationService = validationService;
        _musicBrainzClient = musicBrainzClient;
        _backupManager = backupManager;
        _listenBrainzClient = listenBrainzClient;
        _listensCache = listensCache;
        _playbackTracker = playbackTracker;
    }

    /// <inheritdoc />
    protected override async Task DoHandleAsync(EventData data)
    {
        _logger.LogDebug(
            "Processing user data save event of {ItemName} for user {UserName} with reason {SaveReason}",
            data.Item.Name,
            data.JellyfinUser.Username,
            data.SaveReason);

        switch (data.SaveReason)
        {
            case UserDataSaveReason.UpdateUserRating:
                await HandleFavoriteUpdated(data, CancellationToken.None);
                return;
            case UserDataSaveReason.PlaybackFinished:
                await HandlePlaybackFinished(data, CancellationToken.None);
                return;
            default:
                throw new PluginException("Unsupported data save reason");
        }
    }

    private async Task HandleFavoriteUpdated(EventData data, CancellationToken cancellationToken)
    {
        if (!_configService.IsImmediateFavoriteSyncEnabled)
        {
            _logger.LogDebug("Immediate favorite sync is disabled, skipping sync");
            return;
        }

        await _favoriteSyncService.SyncToListenBrainzAsync(
            data.Item.Id,
            data.JellyfinUser.Id,
            cancellationToken: cancellationToken);
    }

    private async Task HandlePlaybackFinished(EventData data, CancellationToken cancellationToken)
    {
        if (!_configService.IsAlternativeModeEnabled)
        {
            _logger.LogDebug("Alternative mode is disabled, skipping");
            return;
        }

        _logger.LogInformation(
            "Processing playback finished event for {Item}, associated with user {Username}",
            data.Item.Name,
            data.JellyfinUser.Username);

        var userConfig = _configService.GetUserConfig(data.JellyfinUser.Id);
        if (userConfig is null)
        {
            _logger.LogDebug("User config not found, skipping");
            return;
        }

        if (!userConfig.IsListenSubmitEnabled)
        {
            _logger.LogDebug("Listen submission is not enabled for user, skipping");
            return;
        }

        var isValid = ValidateItemRequirements(data.Item, data.JellyfinUser.Id);
        if (!isValid)
        {
            _logger.LogDebug("Item did not meet required conditions, skipping");
            return;
        }

        _logger.LogTrace("All checks passed, preparing to send listen");

        var now = DateUtils.CurrentTimestamp;
        var metadata = await GetMusicBrainzMetadata(data.Item);

        if (_configService.IsBackupEnabled && userConfig.IsBackupEnabled)
        {
            BackupListen(data, userConfig, metadata, now);
        }

        var isOk = await SendListen(userConfig, data.Item, metadata, now, cancellationToken);
        if (!isOk)
        {
            await SaveToListenCache(data.JellyfinUser.Id, data.Item, metadata, now);
            return;
        }

        if (userConfig.IsFavoritesSyncEnabled)
        {
            await _favoriteSyncService.SyncToListenBrainzAsync(
                data.Item.Id,
                data.JellyfinUser.Id,
                cancellationToken: cancellationToken);
        }
    }

    private bool ValidateItemRequirements(Audio item, Guid userId)
    {
        var isAllowed = _validationService.ValidateInAllowedLibrary(item);
        if (!isAllowed)
        {
            _logger.LogTrace("Item is not in an allowed library");
            return false;
        }

        var canSend = _validationService.ValidateBasicMetadata(item);
        if (!canSend)
        {
            _logger.LogTrace("Item does not have minimal metadata for a valid listen");
            return false;
        }

        try
        {
            Monitor.Enter(_userDataSaveLock);
            return ValidatePlaybackCondition(item, userId.ToString());
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Exception occurred while validating playback condition");
            return false;
        }
        finally
        {
            Monitor.Exit(_userDataSaveLock);
        }
    }

    private async Task<AudioItemMetadata?> GetMusicBrainzMetadata(Audio item)
    {
        // TODO: move this into a service (and remove duplicates)

        if (!_configService.IsMusicBrainzEnabled)
        {
            _logger.LogDebug("MusicBrainz integration is disabled, skipping metadata retrieval");
            return null;
        }

        try
        {
            return await _musicBrainzClient.GetAudioItemMetadataAsync(item, CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogDebug("Could not get MusicBrainz metadata: {Message}", e.Message);
            _logger.LogTrace(e, "Exception occurred while getting MusicBrainz metadata");
            return null;
        }
    }

    private void BackupListen(EventData data, UserConfig userConfig, AudioItemMetadata? metadata, long now)
    {
        try
        {
            _logger.LogDebug("Adding listen to backups...");
            _backupManager.Backup(userConfig.UserName, data.Item, metadata, now);
            _logger.LogInformation("Listen successfully backed up");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Listen backup failed: {Reason}", e.Message);
            _logger.LogTrace(e, "Listen backup failed");
        }
    }

    private async Task<bool> SendListen(
        UserConfig userConfig,
        Audio item,
        AudioItemMetadata? metadata,
        long now,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogTrace("Sending listen...");
            await _listenBrainzClient.SendListenAsync(userConfig, item, metadata, now, cancellationToken);
            _logger.LogInformation("Listen successfully sent");
            return true;
        }
        catch (Exception e)
        {
            _logger.LogInformation("Failed to send listen: {Reason}", e.Message);
            _logger.LogTrace(e, "Failed to send listen");
            return false;
        }
    }

    private async Task SaveToListenCache(Guid userId, Audio item, AudioItemMetadata? metadata, long now)
    {
        _logger.LogTrace("Saving listen to cache...");
        await _listensCache.AddListenAsync(userId, item, metadata, now);
        await _listensCache.SaveAsync();
        _logger.LogInformation("Listen has been added to the cache");
    }

    /// <summary>
    /// Evaluate listen submit conditions if the played item is tracked.
    /// </summary>
    /// <param name="item">Item to be tracked.</param>
    /// <param name="userId">ID of the user associated with the listen.</param>
    private bool ValidatePlaybackCondition(Audio item, string userId)
    {
        var trackedItem = _playbackTracker.GetItem(userId, item.Id.ToString());
        if (trackedItem is null)
        {
            _logger.LogDebug("Playback is not tracked for this item, assuming offline playback");
            return true;
        }

        if (!trackedItem.IsValid)
        {
            _logger.LogTrace("Playback tracking is not valid for this item");
            return false;
        }

        var delta = DateUtils.CurrentTimestamp - trackedItem.StartedAt;
        var deltaTicks = delta * TimeSpan.TicksPerSecond;
        var runtime = item.RunTimeTicks ?? 0;
        var isOk = _validationService.ValidateSubmitConditions(deltaTicks, runtime);
        _playbackTracker.InvalidateItem(userId, trackedItem);
        return isOk;
    }
}
