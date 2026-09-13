namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// A ListenBrainz playlist synced to Jellyfin.
/// </summary>
public class PlaylistSyncEntry
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistSyncEntry"/> class.
    /// </summary>
    public PlaylistSyncEntry()
    {
        ListenBrainzPlaylistId = string.Empty;
        Title = string.Empty;
    }

    /// <summary>
    /// Gets or sets the Jellyfin user ID this entry belongs to.
    /// </summary>
    public Guid JellyfinUserId { get; set; }

    /// <summary>
    /// Gets or sets the ListenBrainz playlist ID (MBID).
    /// </summary>
    public string ListenBrainzPlaylistId { get; set; }

    /// <summary>
    /// Gets or sets where the playlist came from.
    /// </summary>
    public PlaylistOrigin Origin { get; set; }

    /// <summary>
    /// Gets or sets the generated playlist type.
    /// Set only if <see cref="Origin"/> is <see cref="PlaylistOrigin.Generated"/>.
    /// </summary>
    public string? GeneratedType { get; set; }

    /// <summary>
    /// Gets or sets the ListenBrainz playlist title as of the last discovery.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the ListenBrainz playlist creation date.
    /// A change means the playlist was regenerated.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin playlist ID.
    /// </summary>
    public Guid JellyfinPlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the last successful sync date.
    /// </summary>
    public DateTime LastSyncedAt { get; set; }
}
