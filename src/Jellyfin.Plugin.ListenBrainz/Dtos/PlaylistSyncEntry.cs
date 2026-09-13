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
    /// Gets or sets corresponding Jellyfin playlist creation date.
    /// </summary>
    public DateTime? SyncedCreatedAt { get; set; }

    /// <summary>
    /// Gets or sets when the playlist was last seen on ListenBrainz.
    /// </summary>
    public DateTime LastSeenAt { get; set; }

    /// <summary>
    /// Gets or sets the Jellyfin playlist ID. Null if the playlist has not been synced yet.
    /// </summary>
    public Guid? JellyfinPlaylistId { get; set; }

    /// <summary>
    /// Gets or sets the last successful sync date. Null if the playlist has never synced.
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
}
