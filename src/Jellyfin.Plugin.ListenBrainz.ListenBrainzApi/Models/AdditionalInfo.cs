using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Models;

/// <summary>
/// Additional <see cref="TrackMetadata"/> info.
/// </summary>
public class AdditionalInfo
{
    /// <summary>
    /// Gets or sets artist MBIDs.
    /// </summary>
    public IEnumerable<string>? ArtistMbids { get; set; }

    /// <summary>
    /// Gets or sets release group MBID.
    /// </summary>
    public string? ReleaseGroupMbid { get; set; }

    /// <summary>
    /// Gets or sets release MBID.
    /// </summary>
    public string? ReleaseMbid { get; set; }

    /// <summary>
    /// Gets or sets recording MBID.
    /// </summary>
    public string? RecordingMbid { get; set; }

    /// <summary>
    /// Gets or sets track MBID.
    /// </summary>
    public string? TrackMbid { get; set; }

    /// <summary>
    /// Gets or sets work MBIDs.
    /// </summary>
    public IEnumerable<string>? WorkMbids { get; set; }

    /// <summary>
    /// Gets or sets track number in a release (album).
    /// Starts from 1.
    /// </summary>
    [JsonPropertyName("tracknumber")]
    public int? TrackNumber { get; set; }

    /// <summary>
    /// Gets or sets ISRC code.
    /// </summary>
    public string? Isrc { get; set; }

    /// <summary>
    /// Gets or sets Spotify URL associated with the recording.
    /// </summary>
    [JsonPropertyName("spotify_id")]
    public string? SpotifyUrl { get; set; }

    /// <summary>
    /// Gets or sets tags.
    /// </summary>
    public IEnumerable<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets name of the media player.
    /// </summary>
    public string? MediaPlayer { get; set; }

    /// <summary>
    /// Gets or sets media player version.
    /// </summary>
    public string? MediaPlayerVersion { get; set; }

    /// <summary>
    /// Gets or sets name of the submission client.
    /// </summary>
    public string? SubmissionClient { get; set; }

    /// <summary>
    /// Gets or sets submission client version.
    /// </summary>
    public string? SubmissionClientVersion { get; set; }

    /// <summary>
    /// Gets or sets canonical domain of an online service.
    /// </summary>
    public string? MusicService { get; set; }

    /// <summary>
    /// Gets or sets name of the online service.
    /// </summary>
    public string? MusicServiceName { get; set; }

    /// <summary>
    /// Gets or sets url of the song/recording (if from online source).
    /// </summary>
    public string? OriginUrl { get; set; }

    /// <summary>
    /// Gets or sets duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }
}
