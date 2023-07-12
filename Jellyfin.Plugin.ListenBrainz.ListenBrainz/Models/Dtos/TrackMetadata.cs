namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Dtos;

/// <summary>
/// ListenBrainz track metadata.
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
    /// <param name="artistName">Name of the artist(s).</param>
    /// <param name="trackName">Name of the track.</param>
    public TrackMetadata(string artistName, string trackName)
    {
        ArtistName = artistName;
        TrackName = trackName;
    }

    /// <summary>
    /// Gets or sets artist name.
    /// </summary>
    public string ArtistName { get; set; }

    /// <summary>
    /// Gets or sets track name.
    /// </summary>
    public string TrackName { get; set; }

    /// <summary>
    /// Gets or sets release (album) name.
    /// </summary>
    public string? ReleaseName { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    public AdditionalInfo? AdditionalInfo { get; set; }
}
