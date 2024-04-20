using Jellyfin.Plugin.ListenBrainz.Utils;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz;

/// <summary>
/// ListenBrainz plugin service for Jellyfin server.
/// </summary>
public sealed class PluginService : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly IUserDataManager _userDataManager;
    private readonly PluginImplementation _plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginService"/> class.
    /// </summary>
    /// <param name="sessionManager">Session manager.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="userDataManager">User data manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    public PluginService(
        ISessionManager sessionManager,
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        IUserDataManager userDataManager,
        ILibraryManager libraryManager,
        IUserManager userManager)
    {
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;

        var listenBrainzLogger = loggerFactory.CreateLogger(Plugin.LoggerCategory + ".ListenBrainzApi");
        var listenBrainzClient = ClientUtils.GetListenBrainzClient(listenBrainzLogger, clientFactory, libraryManager);

        var musicBrainzLogger = loggerFactory.CreateLogger(Plugin.LoggerCategory + ".MusicBrainzApi");
        var musicBrainzClient = ClientUtils.GetMusicBrainzClient(musicBrainzLogger, clientFactory);

        _plugin = new PluginImplementation(
            loggerFactory.CreateLogger(Plugin.LoggerCategory),
            listenBrainzClient,
            musicBrainzClient,
            userDataManager,
            userManager,
            libraryManager);
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += _plugin.OnPlaybackStart;
        _sessionManager.PlaybackStopped += _plugin.OnPlaybackStop;
        _userDataManager.UserDataSaved += _plugin.OnUserDataSave;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= _plugin.OnPlaybackStart;
        _sessionManager.PlaybackStopped -= _plugin.OnPlaybackStop;
        _userDataManager.UserDataSaved -= _plugin.OnUserDataSave;
        return Task.CompletedTask;
    }
}
