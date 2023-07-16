using ListenBrainzPlugin.Clients;
using ListenBrainzPlugin.Extensions;
using ListenBrainzPlugin.Interfaces;
using ListenBrainzPlugin.ListenBrainzApi;
using ListenBrainzPlugin.MusicBrainzApi;
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
    public EntryPoint(ISessionManager sessionManager, ILoggerFactory loggerFactory, IHttpClientFactory clientFactory, IUserDataManager userDataManager)
    {
        _sessionManager = sessionManager;

        var logger = loggerFactory.CreateLogger("ListenBrainzPlugin");
        var listenBrainzClient = GetListenBrainzClient(logger, clientFactory);
        var musicBrainzClient = GetMusicBrainzClient(logger, clientFactory);
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

    private static IListenBrainzClient GetListenBrainzClient(ILogger logger, IHttpClientFactory clientFactory)
    {
        var config = Plugin.GetConfiguration();
        var apiClient = new ListenBrainzApiClient(config.ListenBrainzApiUrl, clientFactory, logger);
        return new ListenBrainzClient(logger, apiClient);
    }

    private static IMetadataClient GetMusicBrainzClient(ILogger logger, IHttpClientFactory clientFactory)
    {
        var config = Plugin.GetConfiguration();
        if (!config.IsMusicBrainzEnabled) return new DummyMusicBrainzClient(logger);

        var clientName = string.Join(string.Empty, Plugin.FullName.Split(' ').Select(s => s.Capitalize()));
        var apiClient = new MusicBrainzApiClient(
            config.MusicBrainzApiUrl,
            clientName,
            Plugin.Version,
            Plugin.SourceUrl,
            clientFactory,
            logger);

        return new MusicBrainzClient(logger, apiClient);
    }
}
