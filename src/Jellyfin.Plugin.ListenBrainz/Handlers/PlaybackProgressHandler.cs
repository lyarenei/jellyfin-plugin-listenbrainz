using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Handlers;

/// <summary>
/// Handler for <see cref="ISessionManager.PlaybackProgress"/> events.
/// </summary>
/// <remarks>
/// Only relevant in alternative mode, where the listen submit conditions cannot be evaluated
/// against the playback position - <see cref="IUserDataManager.UserDataSaved"/> does not carry a
/// usable one.
/// </remarks>
public class PlaybackProgressHandler : GenericHandler<PlaybackProgressEventArgs>
{
    private readonly ILogger _logger;
    private readonly IPluginConfigService _configService;
    private readonly IPlaybackTrackingService _playbackTracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackProgressHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="playbackTracker">Playback tracker instance.</param>
    public PlaybackProgressHandler(
        ILogger logger,
        IUserManager userManager,
        IPluginConfigService configService,
        IPlaybackTrackingService playbackTracker) : base(logger, userManager)
    {
        _logger = logger;
        _configService = configService;
        _playbackTracker = playbackTracker;
    }

    /// <inheritdoc />
    protected override async Task DoHandleAsync(EventData data)
    {
        if (!_configService.IsAlternativeModeEnabled)
        {
            return;
        }

        if (data.PositionTicks is null)
        {
            _logger.LogTrace("Progress event for {ItemName} has no playback position, ignoring", data.Item.Name);
            return;
        }

        var isTracked = await _playbackTracker.UpdatePositionAsync(
            data.JellyfinUser.Id.ToString(),
            data.Item.Id.ToString(),
            data.PositionTicks.Value,
            CancellationToken.None);

        if (!isTracked)
        {
            _logger.LogTrace("Playback of {ItemName} is not tracked, ignoring progress", data.Item.Name);
        }
    }
}
