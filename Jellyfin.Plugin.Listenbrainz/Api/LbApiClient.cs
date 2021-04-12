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
                _logger.LogError($"Failed to submit listen - exception={ex}, name={user.Name}, track={item.Name}");
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
                _logger.LogError($"Failed to submit now listening - exception={ex}, name={user.Name}, track={item.Name}");
            }
        }

        public async Task<ValidateTokenResponse> ValidateToken(string token) => await Get<ValidateTokenRequest, ValidateTokenResponse>(new ValidateTokenRequest(token));

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
                var recordingId = _mbClient?.GetRecordingId(listenRequest.TrackMbId).Result.GetData();
                listenRequest.RecordingMbId = recordingId;
            }

            if (!string.IsNullOrEmpty(item.Artists[0]))
                listenRequest.Artist = item.Artists[0];

            if (!string.IsNullOrEmpty(item.Album))
                listenRequest.Album = item.Album;

            if (!string.IsNullOrEmpty(item.Name))
                listenRequest.Track = item.Name;

            return listenRequest;
        }
    }
}
