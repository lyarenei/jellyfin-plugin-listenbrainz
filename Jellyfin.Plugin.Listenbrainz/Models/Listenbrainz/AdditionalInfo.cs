using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz
{
    /// <summary>
    /// Additional info for <see cref="TrackMetadata"/>.
    /// </summary>
    public class AdditionalInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdditionalInfo"/> class.
        /// </summary>
        public AdditionalInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdditionalInfo"/> class.
        /// </summary>
        /// <param name="recordingMbId">Recording MBID.</param>
        public AdditionalInfo(string recordingMbId)
        {
            RecordingMbId = recordingMbId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AdditionalInfo"/> class.
        /// </summary>
        /// <param name="item">Audio item with source data.</param>
        public AdditionalInfo(Audio item)
        {
            ListeningFrom = "jellyfin";
            TrackNumber = item.IndexNumber;

            if (item.ProviderIds.ContainsKey("MusicBrainzArtist"))
            {
                var artistIds = item.ProviderIds["MusicBrainzArtist"].Split(';');
                ArtistMbIds = new Collection<string>(artistIds);
            }

            if (item.ProviderIds.ContainsKey("MusicBrainzAlbum"))
            {
                ReleaseMbId = item.ProviderIds["MusicBrainzAlbum"];
            }

            if (item.ProviderIds.ContainsKey("MusicBrainzTrack"))
            {
                TrackMbId = item.ProviderIds["MusicBrainzTrack"];
            }

            // If in the future Jellyfin will store Recording MbId
            if (item.ProviderIds.ContainsKey("MusicBrainzRecording"))
            {
                RecordingMbId = item.ProviderIds["MusicBrainzRecording"];
            }
        }

        /// <summary>
        /// Gets or sets the source of listen.
        /// </summary>
        public string? ListeningFrom { get; set; }

        /// <summary>
        /// Gets or sets album MBID.
        /// </summary>
        [JsonPropertyName("release_mbid")]
        public string? ReleaseMbId { get; set; }

        /// <summary>
        /// Gets or sets a collection of artist MBIDs.
        /// </summary>
        [JsonPropertyName("artist_mbids")]
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public Collection<string>? ArtistMbIds { get; set; }

        /// <summary>
        /// Gets or sets recording MBID.
        /// </summary>
        [JsonPropertyName("recording_mbid")]
        public string? RecordingMbId { get; set; }

        /// <summary>
        /// Gets or sets a collection of tags.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public Collection<string>? Tags { get; set; }

        /// <summary>
        /// Gets or sets track MBID.
        /// </summary>
        [JsonPropertyName("track_mbid")]
        public string? TrackMbId { get; set; }

        /// <summary>
        /// Gets or sets work MBID.
        /// </summary>
        public string? WorkMbId { get; set; }

        /// <summary>
        /// Gets or sets track number.
        /// </summary>
        public int? TrackNumber { get; set; }
    }
}
