using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Persistent state for ListenBrainz playlist sync.
/// </summary>
public class PlaylistSyncState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistSyncState"/> class.
    /// </summary>
    public PlaylistSyncState()
    {
        Mappings = [];
    }

    /// <summary>
    /// Gets or sets synced playlist mappings.
    /// </summary>
    [SuppressMessage("Warning", "CA2227", Justification = "Needed for deserialization")]
    public Collection<PlaylistMapping> Mappings { get; set; }

    /// <summary>
    /// Finds the mapping for a given user and ListenBrainz playlist.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="listenBrainzPlaylistId">ListenBrainz playlist ID (MBID).</param>
    /// <returns>The playlist mapping. Null if not found.</returns>
    public PlaylistMapping? FindMapping(Guid userId, string listenBrainzPlaylistId)
    {
        return Mappings.FirstOrDefault(m =>
            m.JellyfinUserId == userId &&
            m.ListenBrainzPlaylistId.Equals(listenBrainzPlaylistId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates or updates the mapping for a ListenBrainz playlist for a given user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="listenBrainzPlaylistId">ListenBrainz playlist ID (MBID).</param>
    /// <param name="jellyfinPlaylistId">Jellyfin playlist ID.</param>
    /// <param name="title">ListenBrainz playlist title at sync time.</param>
    /// <param name="createdAt">ListenBrainz playlist creation date.</param>
    /// <param name="category">Playlist category discriminator.</param>
    /// <returns>The playlist mapping.</returns>
    public PlaylistMapping Upsert(
        Guid userId,
        string listenBrainzPlaylistId,
        Guid jellyfinPlaylistId,
        string title,
        DateTime createdAt,
        string? category)
    {
        var mapping = FindMapping(userId, listenBrainzPlaylistId);
        if (mapping is null)
        {
            mapping = new PlaylistMapping
            {
                JellyfinUserId = userId,
                ListenBrainzPlaylistId = listenBrainzPlaylistId,
            };
            Mappings.Add(mapping);
        }

        mapping.JellyfinPlaylistId = jellyfinPlaylistId;
        mapping.Title = title;
        mapping.CreatedAt = createdAt;
        mapping.Category = category;
        mapping.LastSyncedAt = DateTime.UtcNow;
        return mapping;
    }
}
