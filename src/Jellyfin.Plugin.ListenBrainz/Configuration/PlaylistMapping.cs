namespace Jellyfin.Plugin.ListenBrainz.Configuration;

/// <summary>
/// Mapping between a ListenBrainz playlist and its Jellyfin counterpart.
/// </summary>
public class PlaylistMapping
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistMapping"/> class.
    /// </summary>
    public PlaylistMapping()
    {
        ListenBrainzPlaylistId = string.Empty;
    }

    /// <summary>
    /// Gets or sets the ListenBrainz playlist ID.
    /// </summary>
    public string ListenBrainzPlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin playlist ID.
    /// </summary>
    public Guid JellyfinPlaylistId { get; set; }
}
