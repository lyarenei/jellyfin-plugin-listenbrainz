using Jellyfin.Plugin.ListenBrainz.Common;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Managers;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Handlers;

/// <summary>
/// Handler for <see cref="PlaybackStopEventArgs"/> events.
/// </summary>
public class PlaybackStopHandler : GenericHandler<PlaybackStopEventArgs>
{
    private readonly ILogger _logger;
    private readonly IPluginConfigService _configService;
    private readonly IFavoriteSyncService _favoriteSyncService;
    private readonly IValidationService _validationService;
    private readonly IMetadataProviderService _metadataProvider;
    private readonly IBackupManager _backupManager;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly ListensCacheManager _listensCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStopHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="configService">Plugin config service.</param>
    /// <param name="favoriteSyncService">Favorite sync service.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="metadataProvider">Metadata provider.</param>
    /// <param name="backupManager">Backup manager.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="listensCache">Listen cache manager.</param>
    public PlaybackStopHandler(
        ILogger logger,
        IUserManager userManager,
        IPluginConfigService configService,
        IFavoriteSyncService favoriteSyncService,
        IValidationService validationService,
        IMetadataProviderService metadataProvider,
        IBackupManager backupManager,
        IListenBrainzClient listenBrainzClient,
        ListensCacheManager listensCache) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _favoriteSyncService = favoriteSyncService;
        _validationService = validationService;
        _metadataProvider = metadataProvider;
        _backupManager = backupManager;
        _listenBrainzClient = listenBrainzClient;
        _listensCache = listensCache;
    }

    /// <inheritdoc />
    protected override async Task DoHandleAsync(EventData data)
    {
        if (_configService.IsAlternativeModeEnabled)
        {
            _logger.LogDebug("Alternative mode is enabled, skipping");
            return;
        }

        _logger.LogDebug(
            "Processing playback stop event for {ItemName} associated with user {UserName}",
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

        var isValid = ValidateItemRequirements(data.Item, data.PositionTicks);
        if (!isValid)
        {
            _logger.LogDebug("Item did not meet required conditions, skipping");
            return;
        }

        _logger.LogTrace("All checks passed, preparing to send listen");

        var now = DateUtils.CurrentTimestamp;
        var metadata = await _metadataProvider.GetAudioItemMetadataAsync(data.Item, CancellationToken.None);

        if (_configService.IsBackupEnabled && userConfig.IsBackupEnabled)
        {
            BackupListen(data, userConfig, metadata, now);
        }

        var isOk = await SendListen(userConfig, data.Item, metadata, now, CancellationToken.None);
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
                cancellationToken: CancellationToken.None);
        }
    }

    private bool ValidateItemRequirements(Audio item, long? playbackPositionTicks)
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
            return ValidatePlaybackCondition(item, playbackPositionTicks);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Exception occurred while validating playback condition");
            return false;
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
    /// <param name="playbackPositionTicks">Playback position in ticks.</param>
    private bool ValidatePlaybackCondition(Audio item, long? playbackPositionTicks)
    {
        if (playbackPositionTicks is null)
        {
            _logger.LogDebug("Playback position is not set");
            return false;
        }

        var runtime = item.RunTimeTicks ?? 0;
        return _validationService.ValidateSubmitConditions(playbackPositionTicks.Value, runtime);
    }
}
