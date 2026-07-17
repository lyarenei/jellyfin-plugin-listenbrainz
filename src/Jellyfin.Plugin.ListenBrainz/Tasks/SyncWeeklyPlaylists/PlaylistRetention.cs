namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncWeeklyPlaylists;

/// <summary>
/// Retention strategy of a playlist family.
/// </summary>
internal enum PlaylistRetention
{
    /// <summary>
    /// A rotating set of playlists: only the newest few are kept and out-of-rotation ones are pruned.
    /// </summary>
    Rotation,

    /// <summary>
    /// A permanent archive: every playlist is kept and none are ever pruned.
    /// </summary>
    Archive,
}
