using Newtonsoft.Json;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models;

/// <summary>
/// Playlist track.
/// </summary>
public class PlaylistTrack
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistTrack"/> class.
    /// </summary>
    public PlaylistTrack()
    {
        Identifier = new List<string>();
        Title = string.Empty;
    }

    /// <summary>
    /// Gets or sets track identifier (MusicBrainz recording URL).
    /// For some reason, this is an array.
    /// </summary>
    public IEnumerable<string> Identifier { get; set; }

    /// <summary>
    /// Gets or sets track title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets recording MBID from the identifier URL.
    /// </summary>
    [JsonIgnore]
    public string? RecordingMbid
    {
        get
        {
            // MusicBrainz recording URL is in format
            // https://musicbrainz.org/recording/{mbid}

            var url = Identifier.FirstOrDefault();
            var parts = url?.Split('/');
            return parts?.Length > 1 ? parts[^1] : null;
        }
    }
}
