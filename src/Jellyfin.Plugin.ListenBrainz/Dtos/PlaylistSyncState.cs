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
    /// Gets or sets the playlists to be synced.
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
    /// Adds a discovered playlist, or refreshes an already known one.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="listenBrainzPlaylistId">ListenBrainz playlist ID (MBID).</param>
    /// <param name="origin">Where the playlist came from.</param>
    /// <param name="generatedType">Generated playlist type, if the origin is a generated playlist.</param>
    /// <param name="title">ListenBrainz playlist title.</param>
    /// <param name="createdAt">ListenBrainz playlist creation date.</param>
    /// <returns>The added or refreshed entry.</returns>
    public PlaylistSyncEntry UpsertDiscovered(
        Guid userId,
        string listenBrainzPlaylistId,
        PlaylistOrigin origin,
        string? generatedType,
        string title,
        DateTime createdAt)
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

        entry.Origin = origin;
        entry.GeneratedType = generatedType;
        entry.Title = title;
        entry.CreatedAt = createdAt;
        entry.LastSeenAt = DateTime.UtcNow;
        return entry;
    }

    /// <summary>
    /// Merges results from playlist discovery for a user into the playlist sync states.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="discovered">The playlists discovered for the user.</param>
    /// <returns>The user's entries, in discovery order.</returns>
    public IReadOnlyList<PlaylistSyncEntry> ApplyDiscovery(Guid userId, IEnumerable<DiscoveredPlaylist> discovered)
    {
        return discovered
            .Select(playlist => UpsertDiscovered(
                userId,
                playlist.ListenBrainzPlaylistId,
                playlist.Origin,
                playlist.GeneratedType,
                playlist.Title,
                playlist.CreatedAt))
            .ToList();
    }

    /// <summary>
    /// Records a successful sync on an entry.
    /// </summary>
    /// <param name="entry">The synced entry.</param>
    /// <param name="jellyfinPlaylistId">The ID of the corresponding Jellyfin playlist.</param>
    public static void RecordSync(PlaylistSyncEntry entry, Guid jellyfinPlaylistId)
    {
        var now = DateTime.UtcNow;
        entry.JellyfinPlaylistId = jellyfinPlaylistId;
        entry.LastSyncedAt = now;
        entry.LastAttemptedAt = now;
        entry.SyncedCreatedAt = entry.CreatedAt;
        entry.FailureReason = null;
    }

    /// <summary>
    /// Records a failed sync attempt on an entry, keeping the previous successful sync intact.
    /// </summary>
    /// <param name="entry">The entry which failed to sync.</param>
    /// <param name="error">Why the attempt failed.</param>
    public static void RecordFailure(PlaylistSyncEntry entry, string error)
    {
        ArgumentNullException.ThrowIfNull(entry);

        entry.LastAttemptedAt = DateTime.UtcNow;
        entry.FailureReason = error;
    }

    /// <summary>
    /// Clears the sync result of an entry, marking it as never synced.
    /// </summary>
    /// <param name="entry">The entry to clear.</param>
    public static void ClearSyncResult(PlaylistSyncEntry entry)
    {
        entry.JellyfinPlaylistId = null;
        entry.LastSyncedAt = null;
        entry.SyncedCreatedAt = null;
    }
}
