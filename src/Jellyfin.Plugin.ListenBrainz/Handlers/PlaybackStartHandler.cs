using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Handlers;

/// <summary>
/// Handler for <see cref="ISessionManager.PlaybackStart"/> events.
/// </summary>
public class PlaybackStartHandler : GenericHandler<PlaybackProgressEventArgs>
{
    private readonly ILogger _logger;
    private readonly IValidationService _validationService;
    private readonly IPluginConfigService _configService;
    private readonly IMetadataProviderService _metadataProvider;
    private readonly IListenBrainzService _listenBrainzService;
    private readonly IPlaybackTrackingService _playbackTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="metadataProvider">Metadata provider.</param>
    /// <param name="listenBrainzService">ListenBrainz service.</param>
    /// <param name="playbackTracker">Playback tracker instance.</param>
    /// <param name="userManager">User manager.</param>
    public PlaybackStartHandler(
        ILogger logger,
        IValidationService validationService,
        IPluginConfigService configService,
        IMetadataProviderService metadataProvider,
        IListenBrainzService listenBrainzService,
        IPlaybackTrackingService playbackTracker,
        IUserManager userManager) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _validationService = validationService;
        _metadataProvider = metadataProvider;
        _listenBrainzService = listenBrainzService;
        _playbackTracker = playbackTracker;
    }

    /// <inheritdoc/>
    protected override async Task DoHandleAsync(EventData data)
    {
        _logger.LogInformation(
            "Processing playback start event for {ItemName}, associated with user {UserName}",
            data.Item.Name,
            data.JellyfinUser.Username);

        var userConfig = _configService.GetUserConfig(data.JellyfinUser.Id);
        if (userConfig is null)
        {
            throw new PluginException("No user config found");
        }

        if (!userConfig.IsListenSubmitEnabled)
        {
            throw new PluginException("Listen submission is not enabled for user");
        }

        ValidateItemRequirements(data.Item);

        var metadata = await _metadataProvider.GetAudioItemMetadataAsync(data.Item, CancellationToken.None);
        await _listenBrainzService.SendNowPlayingAsync(userConfig, data.Item, metadata, CancellationToken.None);
        _logger.LogInformation("Successfully sent 'playing now' listen");

        await StartTrackingItemAsync(data.JellyfinUser.Id, data.Item);
    }

    private void ValidateItemRequirements(Audio item)
    {
        var isAllowed = _validationService.ValidateInAllowedLibrary(item);
        if (!isAllowed)
        {
            throw new PluginException("Item is not in an allowed library");
        }

        var canSend = _validationService.ValidateBasicMetadata(item);
        if (!canSend)
        {
            throw new PluginException("Item does not have sufficient metadata for 'playing now' listen");
        }
    }

    private async Task StartTrackingItemAsync(Guid userId, Audio item)
    {
        if (_configService.IsAlternativeModeEnabled)
        {
            _logger.LogDebug("Alternative mode is enabled, adding item to playback tracker");
            await _playbackTracker.AddItemAsync(userId.ToString(), item, CancellationToken.None);
        }
    }
}
