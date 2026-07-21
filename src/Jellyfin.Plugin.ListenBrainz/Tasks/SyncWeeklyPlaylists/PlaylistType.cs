namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncWeeklyPlaylists;

/// <summary>
/// A type of ListenBrainz generated playlist, identified by its source patch.
/// </summary>
internal enum PlaylistType
{
    /// <summary>
    /// Weekly Jams rotation.
    /// </summary>
    Jams,

    /// <summary>
    /// Weekly Exploration rotation.
    /// </summary>
    Exploration,

    /// <summary>
    /// Top Discoveries of the year archive.
    /// </summary>
    TopDiscoveries,

    /// <summary>
    /// Top Missed Recordings of the year archive.
    /// </summary>
    TopMissedRecordings,
}
