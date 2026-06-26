using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using JellyfinPlaylist = MediaBrowser.Controller.Playlists.Playlist;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Manages the Jellyfin-side playlists produced by the ListenBrainz weekly sync.
/// </summary>
public interface IPlaylistManager
{
    /// <summary>
    /// Finds a Jellyfin playlist by ID for the given user.
    /// </summary>
    /// <param name="playlistId">Jellyfin playlist ID.</param>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <returns>The playlist, or null if it does not exist.</returns>
    JellyfinPlaylist? Find(Guid playlistId, Guid userId);

    /// <summary>
    /// Finds a ListenBrainz-tagged Jellyfin playlist by name for the given user.
    /// </summary>
    /// <param name="user">The user the playlist belongs to.</param>
    /// <param name="name">The playlist name.</param>
    /// <returns>The tagged playlist, or null if no match.</returns>
    JellyfinPlaylist? FindByName(User user, string name);

    /// <summary>
    /// Creates a tagged Jellyfin playlist with the given tracks.
    /// </summary>
    /// <param name="user">The user to create the playlist for.</param>
    /// <param name="title">The playlist title.</param>
    /// <param name="tracks">The tracks to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created Jellyfin playlist ID.</returns>
    Task<Guid> CreateAsync(
        User user,
        string title,
        IReadOnlyList<BaseItem> tracks,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces all tracks of an existing playlist.
    /// </summary>
    /// <param name="user">The user the playlist belongs to.</param>
    /// <param name="playlist">The playlist to update.</param>
    /// <param name="tracks">The tracks the playlist should contain.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task.</returns>
    Task ReplaceTracksAsync(
        User user,
        JellyfinPlaylist playlist,
        IReadOnlyList<BaseItem> tracks,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a Jellyfin playlist.
    /// </summary>
    /// <param name="playlist">The playlist to delete.</param>
    void Delete(JellyfinPlaylist playlist);
}
