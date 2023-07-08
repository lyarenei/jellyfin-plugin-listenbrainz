using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Clients;
using Jellyfin.Plugin.Listenbrainz.Configuration;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Extensions;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Services;
using Jellyfin.Plugin.Listenbrainz.Services.ListenCache;
using Jellyfin.Plugin.Listenbrainz.Services.PlaybackTracker;
using Jellyfin.Plugin.Listenbrainz.Utils;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz
{
    /// <summary>
    /// Plugin ServerEntryPoint.
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<ServerEntryPoint> _logger;
        private readonly ListenbrainzClient _apiClient;
        private readonly GlobalConfiguration _globalConfig;
        private readonly IUserManager _userManager;
        private readonly IUserDataManager _userDataManager;
        private readonly IPlaybackTrackerService _playbackTracker;
        private readonly IListenCache _listenCache;

        // Lock for detecting duplicate data saved events
        private static readonly object _dataSavedLock = new();

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
            var config = Plugin.Instance?.Configuration.GlobalConfig;
            _globalConfig = config ?? throw new InvalidOperationException("plugin configuration is NULL");
            _logger = loggerFactory.CreateLogger<ServerEntryPoint>();
            _sessionManager = sessionManager;
            _userManager = userManager;
            _userDataManager = userDataManager;

            _listenCache = new DefaultListenCache(
                Helpers.GetListenCacheFilePath(),
                loggerFactory.CreateLogger<DefaultListenCache>());

            var mbClient = GetMusicBrainzClient(httpClientFactory, loggerFactory);
            _apiClient = GetListenBrainzClient(mbClient, httpClientFactory, loggerFactory);

            _playbackTracker = new DefaultPlaybackTracker(loggerFactory);
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
            _sessionManager.PlaybackStart += PlaybackStart;
            if (_globalConfig.AlternativeListenDetectionEnabled)
                _userDataManager.UserDataSaved += UserDataSaved;
            else
                _sessionManager.PlaybackStopped += PlaybackStopped;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Send "single" listen to Listenbrainz when user data were saved with playback finished reason.
        /// </summary>
        private void UserDataSaved(object? sender, UserDataSaveEventArgs e)
        {
            if (e.Item is not Audio item) return;
            if (e.SaveReason != UserDataSaveReason.PlaybackFinished) return;

            var user = _userManager.GetUserById(e.UserId);
            if (user == null) return;

            var trackedItem = _playbackTracker.GetItem(audio: item, user);
            if (trackedItem != null)
            {
                lock (_dataSavedLock)
                {
                    if (_playbackTracker.GetItem(audio: item, user) is null)
                    {
                        _logger.LogDebug(
                            "Detected duplicate playback report of {Item} (for {User}), ignoring",
                            item.Id,
                            user.Username);
                        return;
                    }

                    _logger.LogDebug(
                        "Found tracking of {Item} (for {User}), will check listen eligibility",
                        item.Id,
                        user.Username);

                    var delta = DateTime.Now - trackedItem.StartedAt;
                    var deltaTicks = delta.TotalSeconds * TimeSpan.TicksPerSecond;
                    try
                    {
                        Limits.EvaluateSubmitConditions((long)deltaTicks, item.RunTimeTicks ?? 0);
                    }
                    catch (ListenBrainzConditionsException ex)
                    {
                        _logger.LogInformation("Listen won't be submitted, conditions have not been met: {Reason}", ex.Message);
                        return;
                    }

                    _playbackTracker.StopTracking(audio: item, user);
                }
            }
            else
            {
                _logger.LogDebug(
                    "No tracking for {Item} (for {User}), assuming offline playback",
                    item.Id,
                    user.Username);
            }

            _logger.LogInformation(
                "Will send listen for {Item}, associated with user {User}",
                item.Name,
                user.Username);

            SendListen(user, item, e.UserData.LastPlayedDate);
        }

        /// <summary>
        /// Send "single" listen to Listenbrainz when playback of item has stopped.
        /// </summary>
        private void PlaybackStopped(object? sender, PlaybackStopEventArgs e)
        {
            if (e.Item is not Audio item) { return; }

            if (e.PlaybackPositionTicks == null)
            {
                _logger.LogDebug("Playback ticks for '{Track}' is null", item.Name);
                return;
            }

            try
            {
                Limits.EvaluateSubmitConditions(e.PlaybackPositionTicks ?? 0, item.RunTimeTicks ?? 0);
            }
            catch (ListenBrainzConditionsException ex)
            {
                _logger.LogInformation("Listen won't be submitted, conditions have not been met: {Reason}", ex.Message);
                return;
            }

            var user = e.Users.FirstOrDefault();
            if (user == null) { return; }

            SendListen(user, item);
        }

        /// <summary>
        /// Send "single" listen to ListenBrainz if appropriate.
        /// </summary>
        /// <param name="user">Jellyfin user.</param>
        /// <param name="item">Audio item.</param>
        /// <param name="datePlayed">When the item has been played.</param>
        private async void SendListen(User user, Audio item, DateTime? datePlayed = null)
        {
            var lbUser = UserHelpers.GetUser(user);
            if (lbUser == null)
            {
                _logger.LogInformation(
                    "Listen won't be sent: " +
                    "could not find Listenbrainz configuration for user '{User}'",
                    user.Username);
                return;
            }

            var (canSubmit, reason) = lbUser.CanSubmitListen();
            if (!canSubmit)
            {
                _logger.LogInformation(
                    "Listen won't be sent for user {User}: {Reason}",
                    user.Username,
                    reason);
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
            var listenRequest = new SubmitListenRequest("single", item, now);

            try
            {
                _apiClient.SubmitListen(lbUser, user, listenRequest);
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
        /// Send "playing_now" listen to Listenbrainz on playback start.
        /// </summary>
        private void PlaybackStart(object? sender, PlaybackProgressEventArgs e)
        {
            if (e.Item is not Audio item) { return; }

            var user = e.Users.FirstOrDefault();
            if (user == null) { return; }

            var lbUser = UserHelpers.GetUser(user);
            if (lbUser == null)
            {
                _logger.LogInformation(
                    "Listen won't be sent: " +
                    "could not find Listenbrainz configuration for user '{User}'",
                    user.Username);
                return;
            }

            var (canSubmit, reason) = lbUser.CanSubmitListen();
            if (!canSubmit)
            {
                _logger.LogInformation(
                    "Listen won't be sent for user {User}: {Reason}",
                    user.Username,
                    reason);
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

            _apiClient.NowPlaying(item, lbUser, user);
            _playbackTracker.StartTracking(audio: item, user: user);
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
            _sessionManager.PlaybackStart -= PlaybackStart;
            if (_globalConfig.AlternativeListenDetectionEnabled)
                _userDataManager.UserDataSaved -= UserDataSaved;
            else
                _sessionManager.PlaybackStopped -= PlaybackStopped;
        }

        private static IMusicbrainzClientService GetMusicBrainzClient(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            var config = Plugin.GetConfiguration();
            if (!config.GlobalConfig.MusicbrainzEnabled)
            {
                return new DummyMusicbrainzClient(loggerFactory.CreateLogger<DummyMusicbrainzClient>());
            }

            var logger = loggerFactory.CreateLogger<MusicbrainzClient>();
            return new MusicbrainzClient(config.MusicBrainzUrl(), httpClientFactory, logger, new SleepService());
        }

        private static ListenbrainzClient GetListenBrainzClient(
            IMusicbrainzClientService mbClient,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory)
        {
            var config = Plugin.GetConfiguration();
            var logger = loggerFactory.CreateLogger<ListenbrainzClient>();
            return new ListenbrainzClient(config.ListenBrainzUrl(), httpClientFactory, mbClient, logger, new SleepService());
        }
    }
}
