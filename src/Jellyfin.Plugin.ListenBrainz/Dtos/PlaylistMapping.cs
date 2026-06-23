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
        Title = string.Empty;
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

    /// <summary>
    /// Gets or sets an optional task-specific category discriminator.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the ListenBrainz playlist title at last sync.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the ListenBrainz playlist creation date.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last sync date.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }
}
