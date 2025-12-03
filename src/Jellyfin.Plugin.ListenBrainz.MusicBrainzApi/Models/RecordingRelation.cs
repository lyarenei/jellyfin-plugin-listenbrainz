namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models;

/// <summary>
/// MusicBrainz recording relation.
/// </summary>
public class RecordingRelation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingRelation"/> class.
    /// </summary>
    public RecordingRelation()
    {
        Recording = new Recording();
    }

    /// <summary>
    /// Gets or sets recording.
    /// </summary>
    public Recording Recording { get; set; }
}
