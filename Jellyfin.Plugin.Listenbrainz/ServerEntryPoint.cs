using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Clients.ListenBrainz;
using Jellyfin.Plugin.Listenbrainz.Clients.MusicBrainz;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Extensions;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Resources.ListenBrainz;
using Jellyfin.Plugin.Listenbrainz.Services;
using Jellyfin.Plugin.Listenbrainz.Services.ListenCache;
using Jellyfin.Plugin.Listenbrainz.Services.PlaybackTracker;
using Jellyfin.Plugin.Listenbrainz.Utils;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz;

/// <summary>
/// Plugin ServerEntryPoint.
/// </summary>
public class ServerEntryPoint : IServerEntryPoint
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<ServerEntryPoint> _logger;
    private readonly ListenBrainzClient _apiClient;
    private readonly IUserDataManager _userDataManager;
    private readonly IListenCache _listenCache;
    private readonly IPlaybackTrackerPlugin _plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerEntryPoint"/> class.
    /// </summary>
    /// <param name="sessionManager">Jellyfin Session manager.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="userDataManager">User data manager.</param>
    public ServerEntryPoint(
        ISessionManager sessionManager,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory,
        IUserManager userManager,
        IUserDataManager userDataManager)
    {
        _logger = loggerFactory.CreateLogger<ServerEntryPoint>();
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;

        _listenCache = new DefaultListenCache(
            Helpers.GetListenCacheFilePath(),
            loggerFactory.CreateLogger<DefaultListenCache>());

        var mbClient = GetMusicBrainzClient(httpClientFactory, loggerFactory);
        _apiClient = GetListenBrainzClient(mbClient, httpClientFactory, loggerFactory);

        _plugin = new ListenBrainzPlugin(
            loggerFactory.CreateLogger<ListenBrainzPlugin>(),
            userManager,
            _apiClient,
            new DefaultPlaybackTracker(loggerFactory),
            _listenCache);
        Instance = this;
    }

    /// <summary>
    /// Gets and sets the plugin instance.
    /// </summary>
    /// <value>The plugin instance.</value>
    public static ServerEntryPoint? Instance { get; private set; }

    /// <summary>
    /// Runs this instance and binds the events to the methods.
    /// </summary>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task RunAsync()
    {
        _sessionManager.PlaybackStart += _plugin.OnPlaybackStarted;

        var config = Plugin.GetConfiguration().GlobalConfig;
        if (config.AlternativeListenDetectionEnabled)
            _userDataManager.UserDataSaved += _plugin.OnUserDataSaved;
        else
            _sessionManager.PlaybackStopped += _plugin.OnPlaybackStopped;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Send "single" listen to ListenBrainz if appropriate.
    /// </summary>
    /// <param name="user">Jellyfin user.</param>
    /// <param name="item">Audio item.</param>
    /// <param name="datePlayed">When the item has been played.</param>
    private async void SendListen(User user, Audio item, DateTime? datePlayed = null)
    {
        var lbUser = UserHelpers.GetListenBrainzUser(user);
        if (lbUser == null)
        {
            _logger.LogInformation(
                "Listen won't be sent: " +
                "could not find ListenBrainz configuration for user '{User}'",
                user.Username);
            return;
        }

        try
        {
            lbUser.CanSubmitListen();
        }
        catch (ListenSubmitException e)
        {
            _logger.LogInformation(
                "Listen won't be sent for user {User}: {Reason}",
                lbUser.Name,
                e.Message);
            return;
        }

        if (!item.HasRequiredMetadata())
        {
            _logger.LogError(
                "Listen won't be sent: " +
                "Track ({Path}) has invalid metadata - missing artist and/or track name",
                item.Path);
            return;
        }

        var now = Helpers.TimestampFromDatetime(datePlayed ?? DateTime.UtcNow);
        try
        {
            _apiClient.SubmitListen(lbUser, ListenType.Single, new Listen(item, now));
        }
        catch (Exception)
        {
            _logger.LogInformation("Listen submission for user {User} failed, persisting listen to cache", user.Username);
            _listenCache.Add(lbUser, new Listen(item, now));
            await _listenCache.SaveToFile();
        }

        if (!lbUser.Options.SyncFavoritesEnabled) { return; }

        string? listenMsId = null;
        const int Retries = 7;
        const int BackOff = 3;
        var waitTime = 1;
        for (int i = 1; i <= Retries; i++)
        {
            listenMsId = await _apiClient.GetMsIdByListenTimestamp(now, lbUser, user).ConfigureAwait(false);
            if (listenMsId != null)
            {
                _logger.LogDebug("Found MSID for {Track} (at {Timestamp}): {MsId}", item.Name, now, listenMsId);
                break;
            }

            _logger.LogDebug(
                "Recording MSID for this listen not found - " +
                "no listens matched for timestamp '{Now}' for user {User}",
                now,
                user.Username);

            if (i + 1 > Retries)
            {
                _logger.LogInformation(
                    "Favorite sync failed: " +
                    "no recording MSID found for listen {Track} (at {Timestamp})",
                    item.Name,
                    now);
                return;
            }

            waitTime *= BackOff;
            _logger.LogDebug("Waiting {Seconds}s before trying again...", waitTime);
            Thread.Sleep(waitTime * 1000);
        }

        Debug.Assert(listenMsId != null, nameof(listenMsId) + " != null");
        _apiClient.SubmitFeedback(item, lbUser, user, listenMsId, item.IsFavoriteOrLiked(user));
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing">If disposing should take place.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposing) return;

        _sessionManager.PlaybackStart -= _plugin.OnPlaybackStarted;

        var config = Plugin.GetConfiguration().GlobalConfig;
        if (config.AlternativeListenDetectionEnabled)
            _userDataManager.UserDataSaved -= _plugin.OnUserDataSaved;
        else
            _sessionManager.PlaybackStopped -= _plugin.OnPlaybackStopped;
    }

    private static IMusicBrainzClient GetMusicBrainzClient(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        var config = Plugin.GetConfiguration();
        if (!config.GlobalConfig.MusicbrainzEnabled)
        {
            return new DummyMusicBrainzClient(loggerFactory.CreateLogger<DummyMusicBrainzClient>());
        }

        var logger = loggerFactory.CreateLogger<DefaultMusicBrainzClient>();
        return new DefaultMusicBrainzClient(config.MusicBrainzUrl, httpClientFactory, logger, new SleepService());
    }

    private static ListenBrainzClient GetListenBrainzClient(
        IMusicBrainzClient mbClient,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        var config = Plugin.GetConfiguration();
        var logger = loggerFactory.CreateLogger<ListenBrainzClient>();
        return new ListenBrainzClient(config.ListenBrainzUrl, httpClientFactory, mbClient, logger, new SleepService());
    }
}
