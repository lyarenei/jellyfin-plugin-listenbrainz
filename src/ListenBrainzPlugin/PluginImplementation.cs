using Jellyfin.Data.Entities;
using ListenBrainzPlugin.Configuration;
using ListenBrainzPlugin.Dtos;
using ListenBrainzPlugin.Exceptions;
using ListenBrainzPlugin.Extensions;
using ListenBrainzPlugin.Interfaces;
using ListenBrainzPlugin.ListenBrainzApi.Resources;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace ListenBrainzPlugin;

/// <summary>
/// ListenBrainz plugin implementation.
/// </summary>
public class PluginImplementation
{
    private readonly ILogger _logger;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly IMetadataClient _metadataClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginImplementation"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="metadataClient">Client for providing additional metadata.</param>
    public PluginImplementation(ILogger logger, IListenBrainzClient listenBrainzClient, IMetadataClient metadataClient)
    {
        _logger = logger;
        _listenBrainzClient = listenBrainzClient;
        _metadataClient = metadataClient;
    }

    /// <summary>
    /// Sends 'playing now' listen to ListenBrainz.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStart(object? sender, PlaybackProgressEventArgs args)
    {
        _logger.LogDebug("Detected playback start for item {Item}", args.Item.Name);
        EventData data;
        try
        {
            data = GetEventData(args);
        }
        catch (Exception e)
        {
            _logger.LogInformation("Cannot handle this event: {Reason}", e.Message);
            _logger.LogDebug(e, "Event data are not valid");
            return;
        }

        var userConfig = data.JellyfinUser.GetListenBrainzConfig();
        if (userConfig is null)
        {
            _logger.LogWarning("Cannot handle this event, user {User} is not configured", data.JellyfinUser.Username);
            return;
        }

        try
        {
            AssertListenBrainzRequirements(data.Item, userConfig);
        }
        catch (Exception e)
        {
            _logger.LogInformation("Cannot handle this event: {Reason}", e.Message);
            _logger.LogDebug(e, "Requirements were not met");
            return;
        }

        AudioItemMetadata? metadata = null;
        try
        {
            metadata = _metadataClient.GetAudioItemMetadata(data.Item).Result;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "No additional metadata available");
            _logger.LogInformation("No additional metadata available: {Reason}", e.Message);
        }

        try
        {
            _listenBrainzClient.SendNowPlaying(userConfig, data.Item, metadata);
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                "Failed to send 'now playing' for user {User}: {Reason}",
                data.JellyfinUser.Username,
                e.Message);

            _logger.LogDebug(e, "Send playing now failed");
        }
    }

    /// <summary>
    /// Sends listen of track to ListenBrainz if conditions have been met.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStop(object? sender, PlaybackStopEventArgs args)
    {
        _logger.LogDebug("Detected playback stop for item {Item}", args.Item.Name);
        EventData data;
        try
        {
            data = GetEventData(args);
        }
        catch (Exception e)
        {
            _logger.LogInformation("Cannot handle this event: {Reason}", e.Message);
            _logger.LogDebug(e, "Event data are not valid");
            return;
        }

        var userConfig = data.JellyfinUser.GetListenBrainzConfig();
        if (userConfig is null)
        {
            _logger.LogWarning("Cannot handle this event, user {User} is not configured", data.JellyfinUser.Username);
            return;
        }

        var position = args.PlaybackPositionTicks;
        if (position is null)
        {
            _logger.LogWarning("Cannot handle this event, playback position is not set");
            return;
        }

        try
        {
            Limits.AssertSubmitConditions((long)position, data.Item.RunTimeTicks ?? 0);
        }
        catch (Exception e)
        {
            throw new ListenBrainzPluginException("Submit conditions were not met", e);
        }

        AudioItemMetadata? metadata = null;
        try
        {
            metadata = _metadataClient.GetAudioItemMetadata(data.Item).Result;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "No additional metadata available");
            _logger.LogInformation("No additional metadata available: {Reason}", e.Message);
        }

        try
        {
            _listenBrainzClient.SendListen(userConfig, data.Item, metadata);
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                "Failed to send listen of {Track} for user {User}: {Reason}",
                data.Item.Name,
                data.JellyfinUser.Username,
                e.Message);

            _logger.LogDebug(e, "Send listen failed");
        }
    }

    private static EventData GetEventData(PlaybackProgressEventArgs eventArgs)
    {
        if (eventArgs.Item is not Audio item)
        {
            throw new ArgumentException("This event is not for an Audio item");
        }

        var jellyfinUser = eventArgs.Users.FirstOrDefault();
        if (jellyfinUser is null)
        {
            throw new ArgumentException("No user is associated with this event");
        }

        return new EventData
        {
            Item = item,
            JellyfinUser = jellyfinUser
        };
    }

    private static void AssertListenBrainzRequirements(Audio item, ListenBrainzUserConfig userConfig)
    {
        try
        {
            item.AssertHasMetadata();
        }
        catch (Exception e)
        {
            throw new ListenBrainzPluginException("Audio item metadata are not valid", e);
        }

        if (userConfig.IsNotListenSubmitEnabled)
        {
            throw new ListenBrainzPluginException("ListenBrainz submission is disabled for this user");
        }
    }

    private struct EventData
    {
        public Audio Item { get; init; }

        public User JellyfinUser { get; init; }
    }
}
