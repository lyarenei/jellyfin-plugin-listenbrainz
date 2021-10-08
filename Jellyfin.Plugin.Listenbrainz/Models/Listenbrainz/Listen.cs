using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models
{
    [DataContract]
    public class Listen
    {
        [DataMember(Name = "listened_at")]
        public int ListenedAt { get; set; }

        [DataMember(Name = "recording_msid")]
        public string RecordingMsid { get; set; }

        [DataMember(Name = "user_name")]
        public string UserName { get; set; }

        [DataMember(Name = "track_metadata")]
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
        [DataMember(Name = "artist_name")]
        public string ArtistName { get; set; }

        [DataMember(Name = "release_name")]
        public string ReleaseName { get; set; }

        [DataMember(Name = "track_name")]
        public string TrackName { get; set; }

        [DataMember(Name = "additional_info")]
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
        [DataMember(Name = "listening_from")]
        public const string ListeningFrom = "jellyfin";

        [DataMember(Name = "artist_mbids")]
        public List<string> ArtistMbIds { get; set; }

        [DataMember(Name = "artist_msid")]
        public string ArtistMsId { get; set; }

        [DataMember(Name = "recording_mbid")]
        public string RecordingMbId { get; set; }

        [DataMember(Name = "recording_msid")]
        public string RecordingMsid { get; set; }

        [DataMember(Name = "release_mbid")]
        public string ReleaseMbId { get; set; }

        [DataMember(Name = "release_msid")]
        public string ReleaseMsid { get; set; }

        [DataMember(Name = "track_mbid")]
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
