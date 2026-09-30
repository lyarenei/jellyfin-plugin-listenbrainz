using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Playlist discovery service.
/// </summary>
public interface IPlaylistDiscoveryService
{
    /// <summary>
    /// Finds the playlists which should be synced for a given user.
    /// </summary>
    /// <param name="userConfig">User configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The discovery result.</returns>
    Task<PlaylistDiscoveryResult> DiscoverAsync(UserConfig userConfig, CancellationToken cancellationToken);
}
