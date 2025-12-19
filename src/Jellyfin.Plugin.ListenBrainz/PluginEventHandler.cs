using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz;

/// <summary>
/// Jellyfin event handler for ListenBrainz plugin.
/// </summary>
public sealed class PluginEventHandler : IDisposable
{
    private readonly ILogger _logger;
    private readonly PluginImplementation _plugin;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginEventHandler"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="plugin">Plugin implementation instance.</param>
    public PluginEventHandler(ILogger logger, PluginImplementation plugin)
    {
        _logger = logger;
        _plugin = plugin;
    }

    ~PluginEventHandler() => Dispose(false);

    /// <summary>
    /// Send listen of track to ListenBrainz if conditions have been met.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStop(object? sender, PlaybackStopEventArgs args)
    {
        using var logScope = BeginLogScope();
        _plugin.OnPlaybackStop(sender, args);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose unmanaged (own) and optionally managed resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _plugin.Dispose();
        }

        _isDisposed = true;
    }

    private IDisposable? BeginLogScope()
    {
        var eventId = Guid.NewGuid().ToString("N")[..7];
        return _logger.BeginScope(new Dictionary<string, object> { { "EventId", eventId } });
    }
}
