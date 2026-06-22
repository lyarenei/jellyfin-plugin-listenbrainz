namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Mapping between a ListenBrainz playlist and a Jellyfin playlist.
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
    /// Gets or sets the Jellyfin user ID.
    /// </summary>
    public Guid JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets the ListenBrainz playlist ID.
    /// </summary>
    public string ListenBrainzPlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin playlist ID.
    /// </summary>
    public Guid JellyfinPlaylistId { get; set; }
}
