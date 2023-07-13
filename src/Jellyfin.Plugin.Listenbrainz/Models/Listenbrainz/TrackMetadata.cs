using System.Text.Json.Serialization;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz
{
    /// <summary>
    /// Track metadata.
    /// </summary>
    public class TrackMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackMetadata"/> class.
        /// </summary>
        public TrackMetadata()
        {
            ArtistName = string.Empty;
            TrackName = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackMetadata"/> class.
        /// </summary>
        /// <param name="item">Audio item with source data.</param>
        public TrackMetadata(Audio item)
        {
            ArtistName = item.Artists[0];
            ReleaseName = item.Album;
            TrackName = item.Name;
            if (item.ProviderIds.ContainsKey("MusicBrainzArtist"))
            {
                Info = new AdditionalInfo(item);
            }
        }

        /// <summary>
        /// Gets or sets artist name.
        /// </summary>
        public string ArtistName { get; set; }

        /// <summary>
        /// Gets or sets album name.
        /// </summary>
        public string? ReleaseName { get; set; }

        /// <summary>
        /// Gets or sets track name.
        /// </summary>
        public string TrackName { get; set; }

        /// <summary>
        /// Gets or sets additional info.
        /// </summary>
        [JsonPropertyName("additional_info")]
        public AdditionalInfo? Info { get; set; }
    }
}
