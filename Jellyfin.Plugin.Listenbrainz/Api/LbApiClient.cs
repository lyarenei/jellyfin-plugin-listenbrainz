using System;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Api
{
    public class LbApiClient : BaseLbApiClient
    {
        private readonly MbClient _mbClient;
        private readonly ILogger _logger;

        public LbApiClient(IHttpClientFactory httpClientFactory, ILogger logger) : base(httpClientFactory, logger)
        {
            _logger = logger;
        }

        public LbApiClient(IHttpClientFactory httpClientFactory, MbClient mbClient, ILogger logger) : base(httpClientFactory, logger)
        {
            _mbClient = mbClient;
            _logger = logger;
        }

        /// <summary>
        /// Submit a listen to Listenbrainz.
        /// </summary>
        /// <param name="item">An audio item which will be submitted. Only necessary if request parameter is null.</param>
        /// <param name="user">Listenbrainz user.</param>
        /// <param name="jfUser">Jellyfin user. Used for logging.</param>
        /// <param name="request">Listen request to submit. Defaults to null. If null, a listen request will be built from audio item.</param>
        /// <returns></returns>
        public async Task SubmitListen(Audio item, LbUser user, User jfUser, SubmitListenRequest request = null)
        {
            var logUsername = $"{jfUser.Username} ({user.Name})";
            var listenRequest = request ?? new SubmitListenRequest(item);
            listenRequest.ApiToken = user.Token;
            listenRequest.ListenType = "single";

            // Workaround for Jellyfin not storing recording ID
            if (string.IsNullOrEmpty(listenRequest.RecordingMbId))
            {
                var recordingId = GetRecordingId(item.Name, listenRequest.TrackMbId);
                if (recordingId != null)
                    listenRequest.RecordingMbId = recordingId;
                else
                    listenRequest.TrackMbId = null;
            }

            try
            {
                var response = await Post<SubmitListenRequest, SubmitListenResponse>(listenRequest);
                if (response != null && !response.IsError())
                {
                    _logger.LogInformation($"{logUsername} listened to '{item.Name}' from album '{item.Album}' by '{item.Artists[0]}'");
                    return;
                }

                _logger.LogError($"Failed to sumbit listen for user {logUsername}: {response.Error}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to submit listen - exception={ex.StackTrace}, name={logUsername}, track={item.Name}");
            }
        }

        /// <summary>
        /// Submit a now playing listen to Listenbrainz.
        /// </summary>
        /// <param name="item">Audio item which will be submitted.</param>
        /// <param name="user">Listenbrainz user.</param>
        /// <param name="jfUser">Jellyfin user. Only used for logging.</param>
        /// <returns></returns>
        public async Task NowPlaying(Audio item, LbUser user, User jfUser)
        {
            var logUsername = $"{jfUser.Username} ({user.Name})";
            var listenRequest = new SubmitListenRequest(item, includeTimestamp: false)
            {
                ApiToken = user.Token,
                ListenType = "playing_now"
            };

            // Workaround for Jellyfin not storing recording ID
            if (string.IsNullOrEmpty(listenRequest.RecordingMbId))
            {
                var recordingId = GetRecordingId(item.Name, listenRequest.TrackMbId);
                if (recordingId != null)
                    listenRequest.RecordingMbId = recordingId;
                else
                    listenRequest.TrackMbId = null;
            }

            try
            {
                var response = await Post<SubmitListenRequest, SubmitListenResponse>(listenRequest);
                if (response != null && !response.IsError())
                {
                    _logger.LogInformation($"{logUsername} is now listening to '{item.Name}' from album '{item.Album}' by '{item.Artists[0]}'");
                    return;
                }

                _logger.LogError($"Failed to submit now listening for user {logUsername}: {response.Error}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to submit now listening - exception={ex.StackTrace}, name={logUsername}, track={item.Name}");
            }
        }

        /// <summary>
        /// Get listens fo a provided user.
        /// </summary>
        /// <param name="user">Listenbrainz user.</param>
        /// <returns>Listens of a provided user, wrapped in a payload response. Null if error.</returns>
        public async Task<UserListensPayload> GetUserListens(LbUser user)
        {
            var request = new UserListensRequest(user.Name)
            {
                ApiToken = user.Token
            };

            try
            {
                var response = await Get<UserListensRequest, UserListensResponse>(request);
                if (response == null)
                {
                    _logger.LogError($"Failed to get listens for user {user.Name}: no response available from server");
                    return null;
                }

                if (response.IsError())
                    _logger.LogError($"Failed to get listens for user {user.Name}: {response.Error}");

                return response.Payload;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed get listens for user - exception={ex.StackTrace}, name={user.Name}");
                return null;
            }
        }

        /// <summary>
        /// Submit a feedback for provided MessyBrainz ID.
        /// </summary>
        /// <param name="item">Audio item. Only used for logging.</param>
        /// <param name="user">Listenbrainz user.</param>
        /// <param name="jfUser">Jellyfin user. Only used for logging.</param>
        /// <param name="msid">MessyBrainz ID of an item.</param>
        /// <param name="isLiked">Audio item is liked.</param>
        /// <returns></returns>
        public async Task SubmitFeedback(Audio item, LbUser user, User jfUser, string msid, bool isLiked)
        {
            var logUsername = $"{jfUser.Username} ({user.Name})";
            var feedbackRequest = new FeedbackRequest()
            {
                // No option for -1 as Jellyfin does not have a concept of dislikes
                Score = isLiked ? 1 : 0,
                RecordingMsid = msid,
                ApiToken = user.Token
            };

            try
            {
                var response = await Post<FeedbackRequest, BaseResponse>(feedbackRequest);
                if (response != null && !response.IsError())
                {
                    _logger.LogInformation($"Submitting user's feedback ({logUsername}) for '{item.Name}'");
                    return;
                }

                _logger.LogError($"Failed to submit user ({logUsername}) feedback: {response.Error}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to submit feedback - exception={ex}, name={logUsername}, track={item.Name}");
            }
        }

        /// <summary>
        /// Validate provided token.
        /// </summary>
        /// <param name="token">Token to validate.</param>
        /// <returns>Response from API.</returns>
        public async Task<ValidateTokenResponse> ValidateToken(string token)
        {
            _logger.LogInformation($"Validating token '{token}'");
            try
            {
                var response = await Get<ValidateTokenRequest, ValidateTokenResponse>(new ValidateTokenRequest(token));
                if (response == null)
                {
                    _logger.LogError($"Validation of token '{token}' failed: no response available from server");
                    return null;
                }

                if (response.IsError())
                {
                    _logger.LogError($"Validation of token '{token}' failed: {response.Message}");
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Token validation failed - exception={ex.StackTrace}, token={token}");
                return null;
            }
        }

        /// <summary>
        /// Retrieve recording MBID by specified track MBID
        /// </summary>
        /// <param name="trackName">Name of the track. Used only for logging.</param>
        /// <param name="trackMbId">MBID of the track.</param>
        /// <returns>Recording MBID.</returns>
        private string GetRecordingId(string trackName, string trackMbId)
        {
            _logger.LogInformation($"Getting Recording ID for Track ID: {trackMbId} ({trackName})");
            var response = _mbClient.GetRecordingId(trackMbId)?.Result;
            if (response == null || response.IsError())
            {
                _logger.LogError($"Failed to retrieve Recording ID for '{trackName}'");
                return null;
            }

            var recordingId = response.GetData();
            if (string.IsNullOrEmpty(recordingId))
            {
                _logger.LogError($"Recording ID for track '{trackName}' not found.");
                return null;
            }

            return recordingId;
        }
    }
}
