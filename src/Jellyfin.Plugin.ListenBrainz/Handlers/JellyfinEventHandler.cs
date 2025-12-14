using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Handlers;

/// <summary>
/// Event handler interface.
/// </summary>
/// <typeparam name="TEventArgs">Event arguments.</typeparam>
public abstract class JellyfinEventHandler<TEventArgs>
    where TEventArgs : EventArgs
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinEventHandler{TEventArgs}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    protected JellyfinEventHandler(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle the event.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event arguments.</param>
    public void HandleEvent(object? sender, TEventArgs args)
    {
        using var logScope = BeginLogScope();
        _logger.LogTrace("Handling event of type {EventType}", typeof(TEventArgs).Name);
        AsyncWrapper(sender, args).Forget();
    }

    /// <summary>
    /// Handle the event.
    /// </summary>
    /// <param name="data">Event data.</param>
    /// <returns>Task handle.</returns>
    /// <exception cref="PluginException">Requirements for operation were not met.</exception>
    protected abstract Task DoHandleAsync(EventData data);

    private async Task AsyncWrapper(object? sender, TEventArgs args)
    {
        try
        {
            var eventData = ParseEventData(sender, args);
            await DoHandleAsync(eventData);
        }
        catch (PluginException e)
        {
            _logger.LogInformation("Event handling finished with error: {ExceptionMessage}", e.Message);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Operation was canceled while handling event");
        }
        catch (Exception e)
        {
            _logger.LogWarning("Exception occurred while handling event: {ExceptionMessage}", e.Message);
            _logger.LogTrace(e, "Could not handle event");
        }
    }

    /// <summary>
    /// Parse event arguments into event data.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    /// <returns>Event data.</returns>
    /// <exception cref="PluginException">Event is not valid.</exception>
    /// <exception cref="ArgumentException">Event is not supported.</exception>
    private static EventData ParseEventData(object? sender, TEventArgs args)
    {
        if (args is not PlaybackProgressEventArgs progressEventArgs)
        {
            throw new ArgumentException("Event type is not supported");
        }

        if (progressEventArgs.Item is not Audio item)
        {
            throw new PluginException("Event is not for an audio item");
        }

        var jellyfinUser = progressEventArgs.Users.FirstOrDefault();
        if (jellyfinUser is null)
        {
            throw new PluginException("No user associated with this event");
        }

        return new EventData
        {
            Item = item,
            JellyfinUser = jellyfinUser
        };
    }

    private IDisposable? BeginLogScope()
    {
        var eventId = Guid.NewGuid().ToString("N")[..7];
        return _logger.BeginScope(new Dictionary<string, object> { { "EventId", eventId } });
    }

    /// <summary>
    /// Convenience struct for event data.
    /// </summary>
    protected struct EventData
    {
        /// <summary>
        /// Gets audio item associated with the event.
        /// </summary>
        public Audio Item { get; init; }

        /// <summary>
        /// Gets jellyfin user associated with the event.
        /// </summary>
        public User JellyfinUser { get; init; }
    }
}
