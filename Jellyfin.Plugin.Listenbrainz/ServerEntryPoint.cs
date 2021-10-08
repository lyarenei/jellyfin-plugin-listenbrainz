using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Api;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz
{
    /// <summary>
    /// Class ServerEntryPoint
    /// </summary>
    public class ServerEntryPoint : IServerEntryPoint
    {
        // Rules for submitting listens: https://listenbrainz.readthedocs.io/en/production/dev/api/#post--1-submit-listens .
        // Listens should be submitted for tracks when the user has listened to half the track or 4 minutes of the track, whichever is lower.
        // If the user hasn’t listened to 4 minutes or half the track, it doesn’t fully count as a listen and should not be submitted.

        // Rule A - if a song reaches >= 4 minutes of playback
        private const long MinimumPlayTimeToSubmitInTicks = 4 * TimeSpan.TicksPerMinute;

        // Rule B - if a song reaches >= 50% played
        private const double MinimumPlayPercentage = 50.00;

        private readonly ISessionManager _sessionManager;
        private readonly IUserDataManager _userDataManager;

        private LbApiClient _apiClient;
        private readonly ILogger<ServerEntryPoint> _logger;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static ServerEntryPoint Instance { get; private set; }

        public ServerEntryPoint(
            ISessionManager sessionManager,
            IJsonSerializer jsonSerializer,
            IHttpClientFactory httpClientFactory,
            ILoggerFactory loggerFactory,
            IUserDataManager userDataManager)
        {
            _logger = loggerFactory.CreateLogger<ServerEntryPoint>();

            _sessionManager = sessionManager;
            _userDataManager = userDataManager;
            var mbClient = new MbClient(httpClientFactory, jsonSerializer, _logger);
            _apiClient = new LbApiClient(httpClientFactory, jsonSerializer, mbClient, _logger);
            Instance = this;
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public Task RunAsync()
        {
            //Bind events
            _sessionManager.PlaybackStart += PlaybackStart;
            _sessionManager.PlaybackStopped += PlaybackStopped;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Let Listenbrainz know when a track has finished.
        /// Playback stopped is run when a track is finished.
        /// </summary>
        private async void PlaybackStopped(object sender, PlaybackStopEventArgs e)
        {
            if (!(e.Item is Audio)) return;

            var item = e.Item as Audio;
            if (e.PlaybackPositionTicks == null)
            {
                _logger.LogDebug($"Playback ticks for '{item.Name}' is null");
                return;
            }

            var playPercent = ((double)e.PlaybackPositionTicks / item.RunTimeTicks) * 100;
            _logger.LogDebug($"Current playback of '{item.Name}': played {playPercent}% ({e.PlaybackPositionTicks} ticks), required {MinimumPlayPercentage}% or {MinimumPlayTimeToSubmitInTicks} ticks");
            if (playPercent < MinimumPlayPercentage && e.PlaybackPositionTicks < MinimumPlayTimeToSubmitInTicks)
            {
                _logger.LogDebug($"Listen to '{item.Name}' won't be submitted, played {playPercent}% ({e.PlaybackPositionTicks} ticks), required {MinimumPlayPercentage}% or {MinimumPlayTimeToSubmitInTicks} ticks");
                return;
            }

            var user = e.Users.FirstOrDefault();
            if (user == null) return;

            var lbUser = Utils.UserHelpers.GetUser(user);
            if (lbUser == null)
            {
                _logger.LogDebug($"Could not find listenbrainz configuration for user '{user.Username}'");
                return;
            }

            if (!lbUser.Options.ListenSubmitEnabled)
            {
                _logger.LogInformation($"User '{user.Username} has submitting of listens turned off.", user.Username);
                return;
            }

            if (string.IsNullOrWhiteSpace(lbUser.Token))
            {
                _logger.LogError($"No API token present for user {user.Username}, aborting");
                return;
            }

            if (string.IsNullOrWhiteSpace(item.Artists[0]) || string.IsNullOrWhiteSpace(item.Name))
            {
                _logger.LogError($"Item {item.Path} has invalid metadata - artist ({item.Artists[0]}, album ({item.Album}), track name ({item.Name})");
                return;
            }

            // TODO jellyfin user name for logs
            // lbUser.Name = user.Username;
            var listenRequest = new ListenRequest(item);
            await _apiClient.SubmitListen(item, lbUser, listenRequest).ConfigureAwait(false);

            // Give Server a bit of time to process new listen submission
            // This is more of a safeguard, rather than a necessity.
            _logger.LogDebug("Waiting 5s before favorite syncing");
            Thread.Sleep(5000);

            if (lbUser.Options.SyncFavoritesEnabled)
            {
                UserListensPayload userListens = await _apiClient.GetUserListens(lbUser);
                if (userListens == null || userListens.Count == 0)
                {
                    _logger.LogError($"Cannot sync favorite for track ({item.Name}), no listens received");
                    return;
                }

                var listen = userListens.Listens.FirstOrDefault(listen => listen.ListenedAt == listenRequest.ListenedAt);
                if (listen != null && listen.ListenedAt == listenRequest.ListenedAt)
                    await _apiClient.SubmitFeedback(item, lbUser, listen.RecordingMsid, item.IsFavoriteOrLiked(user));
                else
                    _logger.LogError($"Could not sync favorite for track ({item.Name}), no match on recent listen.");
            }
        }

        /// <summary>
        /// Let Listenbrainz know when a user has started listening to a track
        /// </summary>
        private async void PlaybackStart(object sender, PlaybackProgressEventArgs e)
        {
            if (!(e.Item is Audio)) return;

            var user = e.Users.FirstOrDefault();
            if (user == null) return;

            var lbUser = Utils.UserHelpers.GetUser(user);
            if (lbUser == null)
            {
                _logger.LogDebug("Could not find listenbrainz user");
                return;
            }

            if (!lbUser.Options.ListenSubmitEnabled)
            {
                _logger.LogInformation($"User '{user.Username} has submitting of listens turned off.");
                return;
            }

            if (string.IsNullOrWhiteSpace(lbUser.Token))
            {
                _logger.LogError($"No API token present for user {user.Username}, aborting");
                return;
            }

            var item = e.Item as Audio;
            if (string.IsNullOrWhiteSpace(item.Artists[0]) || string.IsNullOrWhiteSpace(item.Name))
            {
                _logger.LogError($"Item {item.Path} has invalid metadata - artist ({item.Artists[0]}, album ({item.Album}), track name ({item.Name})");
                return;
            }

            // TODO jellyfin user name for logs
            // lbUser.Name = user.Username;
            await _apiClient.NowPlaying(item, lbUser).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Unbind events
            _sessionManager.PlaybackStart -= PlaybackStart;
            _sessionManager.PlaybackStopped -= PlaybackStopped;

            // Clean up
            _apiClient = null;

        }
    }
}
