using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Utils;
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

        public async Task SubmitListen(Audio item, LbUser user)
        {
            var listenRequest = BuildListenRequest(item);
            listenRequest.ApiToken = user.Token;
            listenRequest.ListenType = "single";

            try
            {
                var response = await Post<ListenRequest, BaseResponse>(listenRequest);
                if (response != null && !response.IsError())
                {
                    _logger.LogInformation($"{user.Name} listened to '{item.Name}' from album '{item.Album}' by '{item.Artists[0]}'");
                    return;
                }

                _logger.LogError($"Failed to sumbit listen for user {user.Name}: {response.Error}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to submit listen - exception={ex.StackTrace}, name={user.Name}, track={item.Name}");
            }
        }

        public async Task NowPlaying(Audio item, LbUser user)
        {
            var listenRequest = BuildListenRequest(item, includeTimestamp: false);
            listenRequest.ApiToken = user.Token;
            listenRequest.ListenType = "playing_now";

            try
            {
                var response = await Post<ListenRequest, BaseResponse>(listenRequest);
                if (response != null && !response.IsError())
                {
                    _logger.LogInformation($"{user.Name} is now listening to '{item.Name}' from album '{item.Album}' by '{item.Artists[0]}'");
                    return;
                }

                _logger.LogError($"Failed to submit now listening for user {user.Name}: {response.Error}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to submit now listening - exception={ex.StackTrace}, name={user.Name}, track={item.Name}");
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
        /// Pull data from item and create ListenRequest instance with them.
        /// </summary>
        /// <param name="item">Audio item containing data.</param>
        /// <param name="includeTimestamp">If timestamp should be included. Defaults to true.</param>
        /// <returns>ListenRequest instace with data.</returns>
        private ListenRequest BuildListenRequest(Audio item, bool includeTimestamp = true)
        {
            var listenRequest = new ListenRequest();

            if (includeTimestamp)
                listenRequest.ListenedAt = Helpers.GetCurrentTimestamp();

            if (item.ProviderIds.ContainsKey("MusicBrainzArtist"))
            {
                var artistIds = item.ProviderIds["MusicBrainzArtist"].Split(';');
                listenRequest.ArtistMbIds = new List<string>(artistIds);
            }
            else
                listenRequest.ArtistMbIds = new List<string>();

            if (item.ProviderIds.ContainsKey("MusicBrainzAlbum"))
                listenRequest.AlbumMbId = item.ProviderIds["MusicBrainzAlbum"];

            if (item.ProviderIds.ContainsKey("MusicBrainzTrack"))
                listenRequest.TrackMbId = item.ProviderIds["MusicBrainzTrack"];

            if (item.ProviderIds.ContainsKey("MusicBrainzRecording"))
                listenRequest.RecordingMbId = item.ProviderIds["MusicBrainzRecording"];

            else if (!string.IsNullOrEmpty(listenRequest.TrackMbId))
            {
                var recordingId = GetRecordingId(item.Name, listenRequest.TrackMbId);
                if (recordingId != null)
                    listenRequest.RecordingMbId = recordingId;
                else
                    listenRequest.TrackMbId = null;
            }

            if (!string.IsNullOrEmpty(item.Artists[0]))
                listenRequest.Artist = item.Artists[0];

            if (!string.IsNullOrEmpty(item.Album))
                listenRequest.Album = item.Album;

            if (!string.IsNullOrEmpty(item.Name))
                listenRequest.Track = item.Name;

            return listenRequest;
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
