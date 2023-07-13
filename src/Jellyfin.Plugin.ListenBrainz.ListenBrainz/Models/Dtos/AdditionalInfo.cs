namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Dtos;

/// <summary>
/// Additional <see cref="TrackMetadata"/> info.
/// </summary>
public class AdditionalInfo
{
    /// <summary>
    /// Gets or sets name of the media player.
    /// </summary>
    public string? MediaPlayer { get; set; }

    /// <summary>
    /// Gets or sets name of the submission client.
    /// </summary>
    public string? SubmissionClient { get; set; }

    /// <summary>
    /// Gets or sets submission client version.
    /// </summary>
    public string? SubmissionClientVersion { get; set; }

    /// <summary>
    /// Gets or sets release MBID.
    /// </summary>
    public string? ReleaseMbid { get; set; }

    /// <summary>
    /// Gets or sets artist MBIDs.
    /// </summary>
    public IEnumerable<string>? ArtistMbids { get; set; }

    /// <summary>
    /// Gets or sets recording MBID.
    /// </summary>
    public string? RecordingMbid { get; set; }

    /// <summary>
    /// Gets or sets tags.
    /// </summary>
    public IEnumerable<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }
}
