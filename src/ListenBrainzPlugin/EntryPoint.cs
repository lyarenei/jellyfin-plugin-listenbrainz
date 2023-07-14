using ListenBrainzPlugin.Interfaces;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace ListenBrainzPlugin;

/// <summary>
/// ListenBrainz plugin entrypoint for Jellyfin server.
/// </summary>
public class EntryPoint : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;
    private readonly IJellyfinPlaybackWatcher _watcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntryPoint"/> class.
    /// </summary>
    /// <param name="sessionManager">Session manager.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public EntryPoint(ISessionManager sessionManager, ILoggerFactory loggerFactory)
    {
        _sessionManager = sessionManager;

        var logger = loggerFactory.CreateLogger<ListenBrainzPlugin>();
        _watcher = new ListenBrainzPlugin(logger);
    }

    /// <inheritdoc />
    public Task RunAsync()
    {
        _sessionManager.PlaybackStart += _watcher.OnPlaybackStart;
        _sessionManager.PlaybackStopped += _watcher.OnPlaybackStop;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sessionManager.PlaybackStart -= _watcher.OnPlaybackStart;
        _sessionManager.PlaybackStopped -= _watcher.OnPlaybackStop;
    }
}
