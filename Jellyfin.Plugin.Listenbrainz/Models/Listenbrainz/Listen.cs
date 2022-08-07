using System.Collections.ObjectModel;
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
    }

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
        public Collection<string>? ArtistMbIds { get; set; }

        /// <summary>
        /// Gets or sets recording MBID.
        /// </summary>
        [JsonPropertyName("recording_mbid")]
        public string? RecordingMbId { get; set; }

        /// <summary>
        /// Gets or sets a collection of tags.
        /// </summary>
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
