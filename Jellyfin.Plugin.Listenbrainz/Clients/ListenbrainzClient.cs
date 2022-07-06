using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Services;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients
{
    /// <summary>
    /// Listenbrainz API client.
    /// </summary>
    public class ListenbrainzClient : BaseListenbrainzClient
    {
        private readonly MusicbrainzClient? _mbClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListenbrainzClient"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="sleepService">Sleep service.</param>
        public ListenbrainzClient(
            IHttpClientFactory httpClientFactory,
            ILogger logger,
            ISleepService sleepService) : base(httpClientFactory, logger, sleepService)
        {
            _mbClient = null;
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ListenbrainzClient"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="mbClient">Musicbrainz API client.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="sleepService">Sleep service.</param>
        public ListenbrainzClient(
            IHttpClientFactory httpClientFactory,
            MusicbrainzClient mbClient,
            ILogger logger,
            ISleepService sleepService) : base(httpClientFactory, logger, sleepService)
        {
            _mbClient = mbClient;
            _logger = logger;
        }

        /// <summary>
        /// Submit a listen.
        /// </summary>
        /// <param name="user">Listenbrainz user.</param>
        /// <param name="jfUser">Jellyfin user. Used for logging.</param>
        /// <param name="request">Listen request to submit.</param>
        public async void SubmitListen(LbUser user, User jfUser, SubmitListenRequest request)
        {
            request.ApiToken = user.Token;

            // Fetch Recording data
            var trackMbId = request.GetTrackMbId();
            if (trackMbId != null)
            {
                if (_mbClient != null)
                {
                    var recordingData = await _mbClient.GetRecordingData(trackMbId).ConfigureAwait(true);
                    if (recordingData != null)
                    {
                        // Set recording MBID as Jellyfin does not store it
                        request.SetRecordingMbId(recordingData.Id);

                        // Set correct artist credit per MusicBrainz entry.
                        request.SetArtist(recordingData.GetCreditString());
                    }
                }
                else
                {
                    _logger.LogDebug("MusicBrainz client not initialized, cannot make requests");
                }
            }
            else
            {
                _logger.LogDebug("No track MBID available, cannot get recording data");
            }

            try
            {
                var response = await Post<SubmitListenRequest, SubmitListenResponse>(request).ConfigureAwait(false);
                if (response != null && !response.IsError())
                {
                    _logger.LogInformation(
                        "User {User} listened to '{Track}' from album '{Album}' by '{Artist}'",
                        jfUser.Username,
                        request.Data[0].Data.TrackName,
                        request.Data[0].Data.ReleaseName,
                        request.Data[0].Data.ArtistName);
                    return;
                }

                _logger.LogWarning("Failed to submit listen for user {User}: {Error}", jfUser.Username, response?.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while submitting listen for user {User}: {Exception}", jfUser.Username, ex.StackTrace);
            }
        }

        /// <summary>
        /// Submit a now playing listen.
        /// </summary>
        /// <param name="item">Audio item which will be submitted.</param>
        /// <param name="user">Listenbrainz user.</param>
        /// <param name="jfUser">Jellyfin user. Used for logging.</param>
        public async void NowPlaying(Audio item, LbUser user, User jfUser)
        {
            var listenRequest = new SubmitListenRequest("playing_now", item) { ApiToken = user.Token };

            // Fetch Recording data
            var trackMbId = listenRequest.GetTrackMbId();
            if (trackMbId != null)
            {
                if (_mbClient != null)
                {
                    var recordingData = await _mbClient.GetRecordingData(trackMbId).ConfigureAwait(true);
                    if (recordingData != null)
                    {
                        // Set recording MBID as Jellyfin does not store it
                        listenRequest.SetRecordingMbId(recordingData.Id);

                        // Set correct artist credit per MusicBrainz entry.
                        listenRequest.SetArtist(recordingData.GetCreditString());
                    }
                }
                else
                {
                    _logger.LogDebug("MusicBrainz client not initialized, cannot make requests");
                }
            }
            else
            {
                _logger.LogDebug("No track MBID available, cannot get recording data");
            }

            try
            {
                var response = await Post<SubmitListenRequest, SubmitListenResponse>(listenRequest).ConfigureAwait(false);
                if (response != null && !response.IsError())
                {
                    _logger.LogInformation(
                        "User {User} is now listening to '{Track}' from album '{Album}' by '{Artists}'",
                        jfUser.Username,
                        item.Name,
                        item.Album,
                        item.Artists[0]);
                    return;
                }

                _logger.LogWarning("Failed to submit now listening for user {User}: {Error}", jfUser.Username, response?.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while submitting now listening for user {User}: {Exception}", jfUser.Username, ex.StackTrace);
            }
        }

        /// <summary>
        /// Get listens for specified user.
        /// </summary>
        /// <param name="user">Listenbrainz user.</param>
        /// <param name="jfUser">Jellyfin user. Used for logging.</param>
        /// <returns>Listens of a provided user, wrapped in a payload response. Null if error.</returns>
        public async Task<UserListensPayload?> GetUserListens(LbUser user, User jfUser)
        {
            var request = new UserListensRequest(user.Name, 30) { ApiToken = user.Token };
            try
            {
                var response = await Get<UserListensRequest, UserListensResponse>(request).ConfigureAwait(false);
                if (response == null)
                {
                    _logger.LogError("Failed to get listens for user {User}: no response available", user.Name);
                    return null;
                }

                if (response.IsError())
                {
                    _logger.LogWarning("Failed to get listens for user {User}: {Error}", jfUser.Username, response.Error);
                    return null;
                }

                return response.Payload;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while getting listens for user {User}: {Exception}", jfUser.Username, ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Submit a feedback for provided Messybrainz ID.
        /// </summary>
        /// <param name="item">Audio item. Only used for logging.</param>
        /// <param name="user">Listenbrainz user.</param>
        /// <param name="jfUser">Jellyfin user. Only used for logging.</param>
        /// <param name="msid">Messybrainz ID of an item.</param>
        /// <param name="isLiked">Audio item is liked.</param>
        public async void SubmitFeedback(Audio item, LbUser user, User jfUser, string msid, bool isLiked)
        {
            var feedbackRequest = new FeedbackRequest
            {
                // No option for -1 as Jellyfin does not have a concept of dislikes
                Score = isLiked ? 1 : 0,
                RecordingMsId = msid,
                ApiToken = user.Token
            };

            try
            {
                var response = await Post<FeedbackRequest, BaseResponse>(feedbackRequest).ConfigureAwait(false);
                if (response != null && !response.IsError())
                {
                    _logger.LogInformation("Successfully submitted feedback for user {User} for track '{Track}'", jfUser.Username, item.Name);
                    return;
                }

                _logger.LogError("Failed to submit feedback for user {User} feedback: {Error}", jfUser.Username, response?.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while submitting feedback for user {User}: {Exception}", jfUser.Username, ex.StackTrace);
            }
        }

        /// <summary>
        /// Validate provided token.
        /// </summary>
        /// <param name="token">Token to validate.</param>
        /// <returns>Response from API.</returns>
        public async Task<ValidateTokenResponse?> ValidateToken(string token)
        {
            _logger.LogDebug("Validating token '{Token}'", token);
            try
            {
                var response = await Get<ValidateTokenRequest, ValidateTokenResponse>(new ValidateTokenRequest(token)).ConfigureAwait(false);
                if (response == null)
                {
                    _logger.LogError("Validation of token '{Token}' failed: no response available from server", token);
                    return null;
                }

                if (response.IsError())
                {
                    _logger.LogWarning("Validation of token '{Token}' failed: {Message}", token, response.Message);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception while validating token {Token}: {Exception}", token, ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Get recording MSID by listen timestamp.
        /// </summary>
        /// <param name="listenedAt">Listen timestamp.</param>
        /// <param name="user">Listenbrainz user.</param>
        /// <param name="jfUser">Jellyfin user. Only used for logging.</param>
        /// <returns>Recording MSID. Null if error or not found.</returns>
        public async Task<string?> GetMsIdByListenTimestamp(long listenedAt, LbUser user, User jfUser)
        {
            var userListens = await GetUserListens(user, jfUser).ConfigureAwait(false);
            if (userListens == null || userListens.Count == 0)
            {
                _logger.LogDebug("No listens received for user {User}", jfUser.Username);
                return null;
            }

            _logger.LogDebug("Expected listen timestamp for favorite sync: {Timestamp}", listenedAt);
            _logger.LogDebug("Received last listen timestamp: {Timestamp}", userListens.LatestListenTs);

            var listen = userListens.Listens.FirstOrDefault(listen => listen.ListenedAt == listenedAt);
            if (listen != null)
            {
                return listen.RecordingMsid;
            }

            _logger.LogDebug("No listen matches expected timestamp");
            return null;
        }
    }
}
