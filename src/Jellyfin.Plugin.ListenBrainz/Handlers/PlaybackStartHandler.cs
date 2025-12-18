using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Managers;
using Jellyfin.Plugin.ListenBrainz.Services;
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
    private readonly DefaultValidationService _defaultValidationService;
    private readonly IPluginConfigService _configService;
    private readonly IMusicBrainzClient _musicBrainzClient;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly PlaybackTrackingManager _playbackTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackStartHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="defaultValidationService">Validation service.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="musicBrainzClient">MusicBrainz client.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="playbackTracker">Playback tracker instance.</param>
    /// <param name="userManager">User manager.</param>
    public PlaybackStartHandler(
        ILogger logger,
        DefaultValidationService defaultValidationService,
        IPluginConfigService configService,
        IMusicBrainzClient musicBrainzClient,
        IListenBrainzClient listenBrainzClient,
        PlaybackTrackingManager playbackTracker,
        IUserManager userManager) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _defaultValidationService = defaultValidationService;
        _musicBrainzClient = musicBrainzClient;
        _listenBrainzClient = listenBrainzClient;
        _playbackTracker = playbackTracker;
    }

    /// <inheritdoc/>
    protected override async Task DoHandleAsync(EventData data)
    {
        _logger.LogDebug(
            "Processing playback start event of {ItemName} for user {UserName}",
            data.Item.Name,
            data.JellyfinUser.Username);

        ValidateItemRequirements(data.Item);
        var userConfig = GetValidUserConfig(data.JellyfinUser.Id);
        var metadata = await GetMusicBrainzMetadata(data.Item);
        await SendPlayingNow(userConfig, data.Item, metadata);

        // TODO: Only in strict mode (new option)
        StartTrackingItem(data.JellyfinUser.Id, data.Item);
    }

    private void ValidateItemRequirements(Audio item)
    {
        var isAllowed = _defaultValidationService.ValidateInAllowedLibrary(item);
        if (!isAllowed)
        {
            throw new PluginException("Item is not in allowed library");
        }

        var canSend = _defaultValidationService.ValidateForPlayingNow(item);
        if (!canSend)
        {
            throw new PluginException("Item does not have sufficient metadata for 'playing now' listen");
        }
    }

    private UserConfig GetValidUserConfig(Guid userId)
    {
        var userConfig = _configService.GetUserConfig(userId);
        if (userConfig is null)
        {
            throw new PluginException("No user config available");
        }

        if (!userConfig.IsListenSubmitEnabled)
        {
            throw new PluginException("Listen submission is not enabled for user");
        }

        return userConfig;
    }

    private async Task<AudioItemMetadata?> GetMusicBrainzMetadata(Audio item)
    {
        if (!_configService.IsMusicBrainzEnabled)
        {
            _logger.LogTrace("MusicBrainz integration is disabled, skipping metadata retrieval");
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

    private async Task SendPlayingNow(UserConfig config, Audio item, AudioItemMetadata? audioMetadata)
    {
        try
        {
            await _listenBrainzClient.SendNowPlayingAsync(config, item, audioMetadata);
            _logger.LogInformation("Successfully sent 'playing now' listen");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Sending 'playing now' listen failed: {Message}", e.Message);
            _logger.LogTrace(e, "Exception occurred while sending 'playing now' listen");
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
