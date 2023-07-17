using ListenBrainzPlugin.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace ListenBrainzPlugin;

/// <summary>
/// ListenBrainz plugin entrypoint for Jellyfin server.
/// </summary>
public sealed class EntryPoint : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;
    private readonly PluginImplementation _plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntryPoint"/> class.
    /// </summary>
    /// <param name="sessionManager">Session manager.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="userDataManager">User data manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    public EntryPoint(
        ISessionManager sessionManager,
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        IUserDataManager userDataManager,
        ILibraryManager libraryManager)
    {
        _sessionManager = sessionManager;

        var logger = loggerFactory.CreateLogger("ListenBrainzPlugin");
        var listenBrainzClient = ClientUtils.GetListenBrainzClient(logger, clientFactory, libraryManager);
        var musicBrainzClient = ClientUtils.GetMusicBrainzClient(logger, clientFactory);
        _plugin = new PluginImplementation(logger, listenBrainzClient, musicBrainzClient, userDataManager);
    }

    /// <inheritdoc />
    public Task RunAsync()
    {
        _sessionManager.PlaybackStart += _plugin.OnPlaybackStart;
        _sessionManager.PlaybackStopped += _plugin.OnPlaybackStop;

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sessionManager.PlaybackStart -= _plugin.OnPlaybackStart;
        _sessionManager.PlaybackStopped -= _plugin.OnPlaybackStop;
    }
}
