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
    private readonly IMetadataProviderService _metadataProvider;
    private readonly IBackupManager _backupManager;
    private readonly IListenBrainzService _listenBrainzService;
    private readonly ListensCacheManager _listensCache;
    private readonly PlaybackTrackingManager _playbackTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserDataSaveHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="configService">Plugin config service.</param>
    /// <param name="favoriteSyncService">Favorite sync service.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="metadataProvider">Metadata provider.</param>
    /// <param name="backupManager">Backup manager.</param>
    /// <param name="listenBrainzService">ListenBrainz service.</param>
    /// <param name="listensCache">Listen cache manager.</param>
    /// <param name="playbackTracker">Playback tracker.</param>
    public UserDataSaveHandler(
        ILogger logger,
        IUserManager userManager,
        IPluginConfigService configService,
        IFavoriteSyncService favoriteSyncService,
        IValidationService validationService,
        IMetadataProviderService metadataProvider,
        IBackupManager backupManager,
        IListenBrainzService listenBrainzService,
        ListensCacheManager listensCache,
        PlaybackTrackingManager playbackTracker) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _favoriteSyncService = favoriteSyncService;
        _validationService = validationService;
        _metadataProvider = metadataProvider;
        _backupManager = backupManager;
        _listenBrainzService = listenBrainzService;
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
                _logger.LogDebug("Unsupported data save reason");
                return;
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
            throw new PluginException("User config not found");
        }

        if (!userConfig.IsListenSubmitEnabled)
        {
            throw new PluginException("Listen submission is not enabled for user");
        }

        ValidateItemRequirements(data.Item, data.JellyfinUser.Id);
        _logger.LogDebug("All checks passed, preparing to send listen");

        var now = DateUtils.CurrentTimestamp;
        var metadata = await _metadataProvider.GetAudioItemMetadataAsync(data.Item, cancellationToken);

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

    private void ValidateItemRequirements(Audio item, Guid userId)
    {
        var isAllowed = _validationService.ValidateInAllowedLibrary(item);
        if (!isAllowed)
        {
            throw new PluginException("Item is not in an allowed library");
        }

        var canSend = _validationService.ValidateBasicMetadata(item);
        if (!canSend)
        {
            throw new PluginException("Item does not have minimal metadata for a valid listen");
        }

        var isOk = ValidatePlaybackCondition(item, userId.ToString());
        if (!isOk)
        {
            throw new PluginException("Playback conditions for listen submission are not met");
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
            _logger.LogDebug(e, "Listen backup failed");
        }
    }

    private async Task<bool> SendListen(
        UserConfig userConfig,
        Audio item,
        AudioItemMetadata? metadata,
        long now,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Sending listen...");
        var isOk = await _listenBrainzService.SendListenAsync(userConfig, item, metadata, now, cancellationToken);
        if (isOk)
        {
            _logger.LogInformation("Listen successfully sent");
        }

        return isOk;
    }

    private async Task SaveToListenCache(Guid userId, Audio item, AudioItemMetadata? metadata, long now)
    {
        _logger.LogDebug("Saving listen to cache...");
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
            _logger.LogDebug("Playback tracking is not valid for this item");
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
