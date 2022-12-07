using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Clients;
using Jellyfin.Plugin.Listenbrainz.Configuration;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Services;
using Jellyfin.Plugin.Listenbrainz.Utils;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz
{
    /// <summary>
    /// Plugin ServerEntryPoint.
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        // Rules for submitting listens: https://listenbrainz.readthedocs.io/en/production/dev/api/#post--1-submit-listens.
        // Listens should be submitted for tracks when the user has listened to half the track or 4 minutes of the track, whichever is lower.
        // If the user hasn't listened to 4 minutes or half the track, it doesn’t fully count as a listen and should not be submitted.

        // Rule A - if a song reaches >= 4 minutes of playback
        private const long MinimumPlayTimeToSubmitInTicks = 4 * TimeSpan.TicksPerMinute;

        // Rule B - if a song reaches >= 50% played
        private const double MinimumPlayPercentage = 50.00;

        private readonly ISessionManager _sessionManager;
        private readonly ILogger<ServerEntryPoint> _logger;
        private readonly ListenbrainzClient _apiClient;
        private readonly GlobalConfiguration _globalConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerEntryPoint"/> class.
        /// </summary>
        /// <param name="sessionManager">Jellyfin Session manager.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="loggerFactory">Logger factory.</param>
        public ServerEntryPoint(
            ISessionManager sessionManager,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory)
        {
            var config = Plugin.Instance?.Configuration.GlobalConfig;
            _globalConfig = config ?? throw new InvalidOperationException("plugin configuration is NULL");
            _logger = loggerFactory.CreateLogger<ServerEntryPoint>();
            _sessionManager = sessionManager;

            IMusicbrainzClientService mbClient;
            if (_globalConfig.MusicbrainzEnabled)
            {
                var mbBaseUrl = _globalConfig.MusicbrainzBaseUrl ?? Resources.Musicbrainz.Api.BaseUrl;
                mbClient = new MusicbrainzClient(mbBaseUrl, httpClientFactory, _logger, new SleepService());
            }
            else
            {
                mbClient = new DummyMusicbrainzClient(_logger);
            }

            var lbBaseUrl = _globalConfig.ListenbrainzBaseUrl ?? Resources.Listenbrainz.Api.BaseUrl;
            _apiClient = new ListenbrainzClient(lbBaseUrl, httpClientFactory, mbClient, _logger, new SleepService());
            Instance = this;
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ServerEntryPoint? Instance { get; private set; }

        /// <summary>
        /// Runs this instance and binds the events to the methods.
        /// </summary>
        /// <returns>A completed <see cref="Task"/>.</returns>
        public Task RunAsync()
        {
            _sessionManager.PlaybackStart += PlaybackStart;
            _sessionManager.PlaybackStopped += PlaybackStopped;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Send "single" listen to Listenbrainz when playback of item has stopped.
        /// </summary>
        private async void PlaybackStopped(object? sender, PlaybackStopEventArgs e)
        {
            if (e.Item is not Audio item)
            {
                return;
            }

            if (e.PlaybackPositionTicks == null)
            {
                _logger.LogDebug("Playback ticks for '{Track}' is null", item.Name);
                return;
            }

            var playPercent = ((double)e.PlaybackPositionTicks / item.RunTimeTicks) * 100;
            _logger.LogDebug(
                "Playback of '{Track}' stopped: played {Percent}% ({Position} ticks), " +
                "required {MinimumPercentage}% or {MinimumPlayTicks} ticks for submission",
                item.Name,
                playPercent,
                e.PlaybackPositionTicks,
                MinimumPlayPercentage,
                MinimumPlayTimeToSubmitInTicks);

            if (playPercent < MinimumPlayPercentage && e.PlaybackPositionTicks < MinimumPlayTimeToSubmitInTicks)
            {
                _logger.LogDebug(
                    "Listen for track '{Track}' won't be submitted, " +
                    "played {Percent}% ({Position} ticks), " +
                    "required {PlayPercentage}% or {MinimumPlayTicks} ticks",
                    item.Name,
                    playPercent,
                    e.PlaybackPositionTicks,
                    MinimumPlayPercentage,
                    MinimumPlayTimeToSubmitInTicks);
                return;
            }

            var user = e.Users.FirstOrDefault();
            if (user == null)
            {
                return;
            }

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

            var now = Helpers.GetCurrentTimestamp();
            var listenRequest = new SubmitListenRequest("single", item, now);
            _apiClient.SubmitListen(lbUser, user, listenRequest);

            if (!lbUser.Options.SyncFavoritesEnabled)
            {
                return;
            }

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
            if (e.Item is not Audio item)
            {
                return;
            }

            var user = e.Users.FirstOrDefault();
            if (user == null)
            {
                return;
            }

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
            if (disposing)
            {
                _sessionManager.PlaybackStart -= PlaybackStart;
                _sessionManager.PlaybackStopped -= PlaybackStopped;
            }
        }
    }
}
