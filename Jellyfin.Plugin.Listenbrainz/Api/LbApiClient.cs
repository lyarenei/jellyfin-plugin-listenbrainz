using System;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Api
{
    public class LbApiClient : BaseLbApiClient
    {
        private readonly MbClient _mbClient;
        private readonly ILogger _logger;

        public LbApiClient(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer, ILogger logger) : base(httpClientFactory, jsonSerializer, logger)
        {
            _logger = logger;
        }

        public LbApiClient(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer, MbClient mbClient, ILogger logger) : base(httpClientFactory, jsonSerializer, logger)
        {
            _mbClient = mbClient;
            _logger = logger;
        }

        public async Task SubmitListen(Audio item, LbUser user, User jfUser, ListenRequest request = null)
        {
            var logUsername = $"{jfUser.Username} ({user.Name})";
            var listenRequest = request ?? new ListenRequest(item);
            listenRequest.ApiToken = user.Token;
            listenRequest.ListenType = "single";

            // Workaround for Jellyfin not storing recording ID
            if (string.IsNullOrEmpty(listenRequest.RecordingMbId))
            {
                var recordingId = GetRecordingId(item.Name, listenRequest.TrackMbId);
                if (recordingId != null) listenRequest.RecordingMbId = recordingId;
                else listenRequest.TrackMbId = null;
            }

            try
            {
                var response = await Post<ListenRequest, BaseResponse>(listenRequest);
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

        public async Task NowPlaying(Audio item, LbUser user, User jfUser)
        {
            var logUsername = $"{jfUser.Username} ({user.Name})";
            var listenRequest = new ListenRequest(item, includeTimestamp: false)
            {
                ApiToken = user.Token,
                ListenType = "playing_now"
            };

            // Workaround for Jellyfin not storing recording ID
            if (string.IsNullOrEmpty(listenRequest.RecordingMbId))
            {
                var recordingId = GetRecordingId(item.Name, listenRequest.TrackMbId);
                if (recordingId != null) listenRequest.RecordingMbId = recordingId;
                else listenRequest.TrackMbId = null;
            }

            try
            {
                var response = await Post<ListenRequest, BaseResponse>(listenRequest);
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
                {
                    _logger.LogError($"Failed to get listens for user {user.Name}: {response.Error}");
                }

                return response.Payload;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed get listens for user - exception={ex.StackTrace}, name={user.Name}");
                return null;
            }
        }

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
        /// Retrieve Recording MBID by Track MBID
        /// </summary>
        /// <param name="trackName">Name of the track</param>
        /// <param name="trackMbId">MBID of the track</param>
        /// <returns>Recording MBID</returns>
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
