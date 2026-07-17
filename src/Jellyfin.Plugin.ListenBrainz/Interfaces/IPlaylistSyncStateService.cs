using Jellyfin.Plugin.ListenBrainz.Dtos;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Service for persisting ListenBrainz playlist sync state.
/// </summary>
public interface IPlaylistSyncStateService
{
    /// <summary>
    /// Reads the playlist sync state.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Playlist sync state.</returns>
    Task<PlaylistSyncState> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Saves the playlist sync state.
    /// </summary>
    /// <param name="state">Weekly playlist sync state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    Task SaveAsync(PlaylistSyncState state, CancellationToken cancellationToken);
}
