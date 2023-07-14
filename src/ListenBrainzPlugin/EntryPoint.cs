using ListenBrainzPlugin.Interfaces;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;

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
    public EntryPoint(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
        _watcher = new ListenBrainzPlugin();
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
