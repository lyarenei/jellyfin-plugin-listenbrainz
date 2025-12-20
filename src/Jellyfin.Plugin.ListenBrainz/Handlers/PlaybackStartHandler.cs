using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Managers;
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
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly PlaybackTrackingManager _playbackTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="validationService">Validation service.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="metadataProvider">Metadata provider.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="playbackTracker">Playback tracker instance.</param>
    /// <param name="userManager">User manager.</param>
    public PlaybackStartHandler(
        ILogger logger,
        IValidationService validationService,
        IPluginConfigService configService,
        IMetadataProviderService metadataProvider,
        IListenBrainzClient listenBrainzClient,
        PlaybackTrackingManager playbackTracker,
        IUserManager userManager) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _validationService = validationService;
        _metadataProvider = metadataProvider;
        _listenBrainzClient = listenBrainzClient;
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
        await SendPlayingNow(userConfig, data.Item, metadata);

        // TODO: Only in strict mode (new option)
        StartTrackingItem(data.JellyfinUser.Id, data.Item);
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

    private async Task SendPlayingNow(UserConfig config, Audio item, AudioItemMetadata? audioMetadata)
    {
        try
        {
            await _listenBrainzClient.SendNowPlayingAsync(config, item, audioMetadata);
            _logger.LogInformation("Successfully sent 'playing now' listen");
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Exception occurred while sending 'playing now' listen");
            throw new PluginException("Failed to send 'playing now' listen", e);
        }
    }

    private void StartTrackingItem(Guid userId, Audio item)
    {
        if (_configService.IsAlternativeModeEnabled)
        {
            _logger.LogDebug("Alternative mode is enabled, adding item to playback tracker");
            _playbackTracker.AddItem(userId.ToString(), item);
        }
    }
}
