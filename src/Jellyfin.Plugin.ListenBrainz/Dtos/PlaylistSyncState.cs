using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Persistent state for ListenBrainz playlist sync.
/// </summary>
/// <remarks>
/// The state is derived from ListenBrainz and user settings, so it is discarded and rebuilt
/// instead of migrated.
/// </remarks>
public class PlaylistSyncState
{
    /// <summary>
    /// Schema version of the state written by this plugin version.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistSyncState"/> class.
    /// </summary>
    public PlaylistSyncState()
    {
        Entries = [];
    }

    /// <summary>
    /// Gets or sets the schema version of this state.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets synced playlist entries.
    /// </summary>
    [SuppressMessage("Warning", "CA2227", Justification = "Needed for deserialization")]
    public Collection<PlaylistSyncEntry> Entries { get; set; }

    /// <summary>
    /// Finds the entry for a given user and ListenBrainz playlist.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="listenBrainzPlaylistId">ListenBrainz playlist ID (MBID).</param>
    /// <returns>The entry, or null if not found.</returns>
    public PlaylistSyncEntry? FindEntry(Guid userId, string listenBrainzPlaylistId)
    {
        return Entries.FirstOrDefault(e =>
            e.JellyfinUserId == userId &&
            e.ListenBrainzPlaylistId.Equals(listenBrainzPlaylistId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all entries belonging to a user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <returns>The user's entries.</returns>
    public IEnumerable<PlaylistSyncEntry> EntriesFor(Guid userId)
    {
        return Entries.Where(e => e.JellyfinUserId == userId);
    }

    /// <summary>
    /// Creates or updates the entry for a ListenBrainz playlist for a given user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="listenBrainzPlaylistId">ListenBrainz playlist ID (MBID).</param>
    /// <param name="jellyfinPlaylistId">Jellyfin playlist ID.</param>
    /// <param name="title">ListenBrainz playlist title at sync time.</param>
    /// <param name="createdAt">ListenBrainz playlist creation date.</param>
    /// <param name="origin">Where the playlist came from.</param>
    /// <param name="generatedType">Generated playlist type, if the origin is a generated playlist.</param>
    /// <returns>The playlist entry.</returns>
    public PlaylistSyncEntry Upsert(
        Guid userId,
        string listenBrainzPlaylistId,
        Guid jellyfinPlaylistId,
        string title,
        DateTime createdAt,
        PlaylistOrigin origin,
        string? generatedType)
    {
        var entry = FindEntry(userId, listenBrainzPlaylistId);
        if (entry is null)
        {
            entry = new PlaylistSyncEntry
            {
                JellyfinUserId = userId,
                ListenBrainzPlaylistId = listenBrainzPlaylistId,
            };
            Entries.Add(entry);
        }

        entry.JellyfinPlaylistId = jellyfinPlaylistId;
        entry.Title = title;
        entry.CreatedAt = createdAt;
        entry.Origin = origin;
        entry.GeneratedType = generatedType;
        entry.LastSyncedAt = DateTime.UtcNow;
        return entry;
    }
}
