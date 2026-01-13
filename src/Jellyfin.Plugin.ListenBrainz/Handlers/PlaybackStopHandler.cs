using Jellyfin.Plugin.ListenBrainz.Common;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
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
    private readonly IListenBackupService _backupService;
    private readonly IListenBrainzService _listenBrainzService;
    private readonly IListensCachingService _listensCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStopHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="configService">Plugin config service.</param>
    /// <param name="favoriteSyncService">Favorite sync service.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="metadataProvider">Metadata provider.</param>
    /// <param name="backupService">Backup service.</param>
    /// <param name="listenBrainzService">ListenBrainz service.</param>
    /// <param name="listensCache">Listen cache manager.</param>
    public PlaybackStopHandler(
        ILogger logger,
        IUserManager userManager,
        IPluginConfigService configService,
        IFavoriteSyncService favoriteSyncService,
        IValidationService validationService,
        IMetadataProviderService metadataProvider,
        IListenBackupService backupService,
        IListenBrainzService listenBrainzService,
        IListensCachingService listensCache) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _favoriteSyncService = favoriteSyncService;
        _validationService = validationService;
        _metadataProvider = metadataProvider;
        _backupService = backupService;
        _listenBrainzService = listenBrainzService;
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

        _logger.LogInformation(
            "Processing playback stop event for {ItemName} associated with user {UserName}",
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

        ValidateItemRequirements(data.Item, data.PositionTicks);
        _logger.LogDebug("All checks passed, preparing to send listen");

        var now = DateUtils.CurrentTimestamp;
        var metadata = await _metadataProvider.GetAudioItemMetadataAsync(data.Item, CancellationToken.None);

        if (_configService.IsBackupEnabled && userConfig.IsBackupEnabled)
        {
            await BackupListen(data, userConfig, metadata, now, CancellationToken.None);
        }

        var isOk = false;
        try
        {
            isOk = await SendListen(userConfig, data.Item, metadata, now, CancellationToken.None);
        }
        finally
        {
            if (!isOk)
            {
                await SaveToListenCache(data.JellyfinUser.Id, data.Item, metadata, now);
            }
        }

        if (isOk && userConfig.IsFavoritesSyncEnabled)
        {
            await _favoriteSyncService.SyncToListenBrainzAsync(
                data.Item.Id,
                data.JellyfinUser.Id,
                cancellationToken: CancellationToken.None);
        }
    }

    private void ValidateItemRequirements(Audio item, long? playbackPositionTicks)
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

        if (playbackPositionTicks is null)
        {
            throw new PluginException("Playback position is not set");
        }

        var runtime = item.RunTimeTicks ?? 0;
        var isValid = _validationService.ValidateSubmitConditions(playbackPositionTicks.Value, runtime);
        if (!isValid)
        {
            throw new PluginException("Playback time does not meet listen submission conditions");
        }
    }

    private async Task BackupListen(
        EventData data,
        UserConfig userConfig,
        AudioItemMetadata? metadata,
        long now,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Adding listen to backups...");
            await _backupService.Backup(userConfig.UserName, data.Item, metadata, now, cancellationToken);
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
}
