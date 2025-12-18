using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Handlers;

/// <summary>
/// A generic handler for Jellyfin events.
/// </summary>
/// <typeparam name="TEventArgs">Event arguments.</typeparam>
public abstract class GenericHandler<TEventArgs>
    where TEventArgs : EventArgs
{
    private readonly ILogger _logger;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenericHandler{TEventArgs}"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager.</param>
    protected GenericHandler(ILogger logger, IUserManager userManager)
    {
        _logger = logger;
        _userManager = userManager;
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
    private EventData ParseEventData(object? sender, TEventArgs args)
    {
        return args switch
        {
            PlaybackProgressEventArgs eventArgs => ParseEventArgs(sender, eventArgs),
            UserDataSaveEventArgs eventArgs => ParseEventArgs(sender, eventArgs),
            _ => throw new ArgumentException("Event type is not supported"),
        };
    }

    private static EventData ParseEventArgs(object? sender, PlaybackProgressEventArgs args)
    {
        if (args.Item is not Audio item)
        {
            throw new PluginException("Event is not for an audio item");
        }

        var jellyfinUser = args.Users.FirstOrDefault();
        if (jellyfinUser is null)
        {
            throw new PluginException("No user associated with this event");
        }

        return new EventData
        {
            Item = item,
            JellyfinUser = jellyfinUser,
        };
    }

    private EventData ParseEventArgs(object? sender, UserDataSaveEventArgs args)
    {
        if (args.Item is not Audio item)
        {
            throw new PluginException("Event is not for an audio item");
        }

        switch (args.SaveReason)
        {
            case UserDataSaveReason.PlaybackFinished:
            case UserDataSaveReason.UpdateUserRating:
                break;
            default:
                throw new PluginException("Event save reason is not supported");
        }

        var jellyfinUser = _userManager.GetUserById(args.UserId);
        if (jellyfinUser is null)
        {
            throw new PluginException("No user associated with this event");
        }

        return new EventData
        {
            Item = item,
            JellyfinUser = jellyfinUser,
            SaveReason = args.SaveReason,
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

        /// <summary>
        /// Gets the reason for user data save.
        /// Only set if event data are parsed from <see cref="UserDataSaveEventArgs"/>.
        /// </summary>
        public UserDataSaveReason? SaveReason { get; init; }
    }
}
