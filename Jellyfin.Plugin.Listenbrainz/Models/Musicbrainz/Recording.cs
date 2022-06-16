namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz;

/// <summary>
/// Recording model.
/// </summary>
public class Recording
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Recording"/> class.
    /// </summary>
    public Recording()
    {
        Id = string.Empty;
    }

    /// <summary>
    /// Gets or sets recording MBID.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets recording match score.
    /// </summary>
    public int Score { get; set; }
}
