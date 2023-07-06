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
        /// Gets or sets listenbrainz username.
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
        /// Sets recording MBID.
        /// </summary>
        [JsonIgnore]
        public string RecordingMBID
        {
            set
            {
                if (Data.Info != null) Data.Info.RecordingMbId = value;
            }
        }

        /// <summary>
        /// Sets artist credit string.
        /// </summary>
        [JsonIgnore]
        public string ArtistCredit
        {
            set => Data.ArtistName = value;
        }
    }
}
