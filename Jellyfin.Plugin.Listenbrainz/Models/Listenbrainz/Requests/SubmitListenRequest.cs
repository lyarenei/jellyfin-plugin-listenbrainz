using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Jellyfin.Plugin.Listenbrainz.Utils;
using MediaBrowser.Controller.Entities.Audio;
using static Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    [DataContract]
    public class SubmitListenRequest : BaseRequest
    {
        public string ListenType { get; set; }
        public long? ListenedAt { get; set; }

        public string Artist { get; set; }
        public List<string> ArtistMbIds { get; set; }

        public string Album { get; set; }
        public string AlbumMbId { get; set; }

        public string Track { get; set; }
        public string TrackMbId { get; set; }
        public string RecordingMbId { get; set; }

        public SubmitListenRequest() { }

        public SubmitListenRequest(Audio item, bool includeTimestamp = true)
        {
            if (includeTimestamp)
                ListenedAt = Helpers.GetCurrentTimestamp();

            if (item.ProviderIds.ContainsKey("MusicBrainzArtist"))
            {
                var artistIds = item.ProviderIds["MusicBrainzArtist"].Split(';');
                ArtistMbIds = new List<string>(artistIds);
            }
            else
                ArtistMbIds = new List<string>();

            if (item.ProviderIds.ContainsKey("MusicBrainzAlbum"))
                AlbumMbId = item.ProviderIds["MusicBrainzAlbum"];

            if (item.ProviderIds.ContainsKey("MusicBrainzTrack"))
                TrackMbId = item.ProviderIds["MusicBrainzTrack"];

            // Note: Jellyfin does not store Recording MbId, only TrackMbId
            if (item.ProviderIds.ContainsKey("MusicBrainzRecording"))
                RecordingMbId = item.ProviderIds["MusicBrainzRecording"];

            if (!string.IsNullOrEmpty(item.Artists[0]))
                Artist = item.Artists[0];

            if (!string.IsNullOrEmpty(item.Album))
                Album = item.Album;

            if (!string.IsNullOrEmpty(item.Name))
                Track = item.Name;
        }

        public override Dictionary<string, dynamic> ToRequestForm()
        {
            var metadata = new Dictionary<string, dynamic>
            {
                { "listening_from", "jellyfin" }
            };

            if (ArtistMbIds.Any())
                metadata.Add("artist_mbids", ArtistMbIds);

            if (!string.IsNullOrEmpty(AlbumMbId))
                metadata.Add("release_mbid", AlbumMbId);

            if (!string.IsNullOrEmpty(TrackMbId))
                metadata.Add("track_mbid", TrackMbId);

            // Note: Recording is needed to link the tracks in user activity,
            // but Jellyfin does not store recording IDs atm
            if (!string.IsNullOrEmpty(RecordingMbId))
                metadata.Add("recording_mbid", RecordingMbId);

            var trackData = new Dictionary<string, dynamic>
            {
                { "additional_info", metadata }
            };

            if (!string.IsNullOrEmpty(Artist))
                trackData.Add("artist_name", Artist);

            if (!string.IsNullOrEmpty(Album))
                trackData.Add("release_name", Album);

            if (!string.IsNullOrEmpty(Track))
                trackData.Add("track_name", Track);

            var payload = new Dictionary<string, dynamic>
            {
                { "track_metadata", trackData }
            };

            if (ListenedAt != null)
                payload.Add("listened_at", ListenedAt);

            return new Dictionary<string, dynamic>
            {
                { "listen_type", ListenType },
                { "payload", new List<Dictionary<string, dynamic>> { payload } }
            };
        }

        public override string GetEndpoint() => Endpoints.SubmitListen;
    }
}
