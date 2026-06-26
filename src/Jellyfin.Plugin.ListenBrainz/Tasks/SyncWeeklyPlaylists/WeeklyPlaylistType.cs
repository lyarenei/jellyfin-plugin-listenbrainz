namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncWeeklyPlaylists;

/// <summary>
/// ListenBrainz weekly playlist family.
/// </summary>
internal enum WeeklyPlaylistType
{
    /// <summary>
    /// Weekly Jams rotation.
    /// </summary>
    Jams,

    /// <summary>
    /// Weekly Exploration rotation.
    /// </summary>
    Exploration,
}
