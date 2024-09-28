using Jellyfin.Plugin.ListenBrainz.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Common;

/// <summary>
/// Extensions for <see cref="ILogger"/>.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Add a new scope for specified event ID.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="eventKey">Event key. Defaults to "EventId" if null.</param>
    /// <param name="eventVal">Event value. Defaults to a value from <see cref="Utils.GetNewId"/> if null.</param>
    /// <returns>Disposable logger scope.</returns>
    public static IDisposable AddNewScope(this ILogger logger, string? eventKey = null, string? eventVal = null)
    {
        var key = eventKey ?? "EventId";
        var val = eventVal ?? Utils.GetNewId();
        var scopedLogger = logger.BeginScope(new Dictionary<string, object> { { key, val } });
        return scopedLogger ?? throw new FatalException("Failed to initialize logger");
    }
}
