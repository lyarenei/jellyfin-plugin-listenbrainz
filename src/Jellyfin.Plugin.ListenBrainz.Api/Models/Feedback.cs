namespace Jellyfin.Plugin.ListenBrainz.Api.Models;

/// <summary>
/// ListenBrainz feedback model.
/// </summary>
public class Feedback
{
    /// <summary>
    /// Gets or sets recording MBID.
    /// </summary>
    public string? RecordingMbid { get; set; }
}
