using System.Text.Json.Serialization;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz
{
    /// <summary>
    /// Listen model.
    /// </summary>
    public class Listen
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Listen"/> class.
        /// </summary>
        public Listen()
        {
            Data = new TrackMetadata();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Listen"/> class.
        /// </summary>
        /// <param name="item">Audio item with data.</param>
        /// <param name="listenedAt">Listened at UNIX timestamp.</param>
        public Listen(Audio item, long? listenedAt = null)
        {
            ListenedAt = listenedAt;
            Data = new TrackMetadata(item);
        }

        /// <summary>
        /// Gets or sets unix timestamp of listen.
        /// </summary>
        public long? ListenedAt { get; set; }

        /// <summary>
        /// Gets or sets listen track metadata.
        /// </summary>
        [JsonPropertyName("track_metadata")]
        public TrackMetadata Data { get; set; }

        /// <summary>
        /// Gets or sets recording MSID.
        /// </summary>
        [JsonPropertyName("recording_msid")]
        public string? RecordingMsid { get; set; }

        /// <summary>
        /// Gets or sets ListenBrainz username.
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Gets track MBID.
        /// </summary>
        [JsonIgnore]
        public string? TrackMBID
        {
            get => Data.Info?.TrackMbId;
        }

        /// <summary>
        /// Gets or sets recording MBID.
        /// </summary>
        /// <param name="value">Recording MBID.</param>
        [JsonIgnore]
        public string? RecordingMBID
        {
            get => Data.Info?.RecordingMbId;
            set
            {
                if (Data.Info != null) { Data.Info.RecordingMbId = value; }
            }
        }

        /// <summary>
        /// Convenience method for setting artist credit string.
        /// </summary>
        /// <param name="value">Artist credit string.</param>
        public void SetArtistCredit(string value) => Data.ArtistName = value;
    }
}
