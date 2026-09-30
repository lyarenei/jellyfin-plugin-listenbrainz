namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Playlist sync discovery result.
/// </summary>
/// <param name="Playlists">The playlists selected for syncing.</param>
/// <param name="IsComplete">All playlists have been discovered.</param>
public sealed record PlaylistDiscoveryResult(IReadOnlyList<DiscoveredPlaylist> Playlists, bool IsComplete)
{
    /// <summary>
    /// Gets the result of a failed discovery pass.
    /// </summary>
    public static PlaylistDiscoveryResult Failed { get; } = new([], false);
}
