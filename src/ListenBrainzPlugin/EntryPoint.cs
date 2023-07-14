using ListenBrainzPlugin.Clients;
using ListenBrainzPlugin.Interfaces;
using ListenBrainzPlugin.ListenBrainzApi;
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
    /// <param name="clientFactory">HTTP client factory.</param>
    public EntryPoint(ISessionManager sessionManager, ILoggerFactory loggerFactory, IHttpClientFactory clientFactory)
    {
        _sessionManager = sessionManager;

        var logger = loggerFactory.CreateLogger<ListenBrainzPlugin>();
        var listenBrainzClient = GetListenBrainzClient(logger, clientFactory);
        _watcher = new ListenBrainzPlugin(logger, listenBrainzClient);
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

    private static IListenBrainzClient GetListenBrainzClient(ILogger logger, IHttpClientFactory clientFactory)
    {
        var config = Plugin.GetConfiguration();
        var apiClient = new ListenBrainzApiClient(config.ListenBrainzApiUrl, clientFactory, logger);
        return new ListenBrainzClient(logger, apiClient);
    }
}
