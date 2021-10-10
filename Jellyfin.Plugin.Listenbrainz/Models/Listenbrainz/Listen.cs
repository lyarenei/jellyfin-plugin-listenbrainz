using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models
{
    public class Listen
    {
        [JsonPropertyName("listened_at")]
        public int ListenedAt { get; set; }

        [JsonPropertyName("recording_msid")]
        public string RecordingMsid { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("track_metadata")]
        public TrackMetadata TrackMetadata { get; set; }

        public Dictionary<string, dynamic> ToRequestForm()
        {
            var dict = new Dictionary<string, dynamic>();
            if (ListenedAt != 0) dict.Add("listened_at", ListenedAt);
            if (!string.IsNullOrEmpty(RecordingMsid)) dict.Add("recording_msid", RecordingMsid);
            if (TrackMetadata != null) dict.Add("track_metadata", TrackMetadata.ToRequestForm());
            return dict;
        }
    }

    [DataContract]
    public class TrackMetadata
    {
        [JsonPropertyName("artist_name")]
        public string ArtistName { get; set; }

        [JsonPropertyName("release_name")]
        public string ReleaseName { get; set; }

        [JsonPropertyName("track_name")]
        public string TrackName { get; set; }

        [JsonPropertyName("additional_info")]
        public AdditionalInfo AdditionalInfo { get; set; }

        public Dictionary<string, dynamic> ToRequestForm()
        {
            var dict = new Dictionary<string, dynamic>();
            if (!string.IsNullOrEmpty(ArtistName)) dict.Add("artist_name", ArtistName);
            if (!string.IsNullOrEmpty(ReleaseName)) dict.Add("release_name", ReleaseName);
            if (!string.IsNullOrEmpty(TrackName)) dict.Add("track_name", TrackName);
            if (AdditionalInfo != null) dict.Add("additional_info", AdditionalInfo.ToRequestForm());
            return dict;
        }
    }

    [DataContract]
    public class AdditionalInfo
    {
        [JsonPropertyName("listening_from")]
        public const string ListeningFrom = "jellyfin";

        [JsonPropertyName("artist_mbids")]
        public List<string> ArtistMbIds { get; set; }

        [JsonPropertyName("artist_msid")]
        public string ArtistMsId { get; set; }

        [JsonPropertyName("recording_mbid")]
        public string RecordingMbId { get; set; }

        [JsonPropertyName("recording_msid")]
        public string RecordingMsid { get; set; }

        [JsonPropertyName("release_mbid")]
        public string ReleaseMbId { get; set; }

        [JsonPropertyName("release_msid")]
        public string ReleaseMsid { get; set; }

        [JsonPropertyName("track_mbid")]
        public string TrackMbId { get; set; }

        public Dictionary<string, dynamic> ToRequestForm()
        {
            var dict = new Dictionary<string, dynamic>
        {
          { "listening_from", ListeningFrom }
        };

            if (ArtistMbIds.Any()) dict.Add("artist", ArtistMbIds);
            if (!string.IsNullOrEmpty(ArtistMsId)) dict.Add("artist_msid", ArtistMsId);
            if (!string.IsNullOrEmpty(RecordingMbId)) dict.Add("recording_mbid", RecordingMbId);
            if (!string.IsNullOrEmpty(RecordingMsid)) dict.Add("recording_msid", RecordingMsid);
            if (!string.IsNullOrEmpty(ReleaseMbId)) dict.Add("release_mbid", ReleaseMbId);
            if (!string.IsNullOrEmpty(ReleaseMsid)) dict.Add("release_msid", ReleaseMsid);
            if (!string.IsNullOrEmpty(TrackMbId)) dict.Add("track_mbid", TrackMbId);
            return dict;
        }
    }
}
