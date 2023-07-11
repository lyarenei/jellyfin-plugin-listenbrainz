namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Dtos;

/// <summary>
/// ListenBrainz Listen object.
/// </summary>
public class Listen
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Listen"/> class.
    /// </summary>
    /// <param name="artistName">Full credit string for artists of the track.</param>
    /// <param name="trackName">Track name.</param>
    public Listen(string artistName, string trackName)
    {
        TrackMetadata = new TrackMetadata(artistName, trackName);
    }

    /// <summary>
    /// Gets or sets UNIX timestamp of the listen.
    /// </summary>
    public long? ListenedAt { get; set; }

    /// <summary>
    /// Gets or sets track metadata.
    /// </summary>
    public TrackMetadata TrackMetadata { get; set; }
}
