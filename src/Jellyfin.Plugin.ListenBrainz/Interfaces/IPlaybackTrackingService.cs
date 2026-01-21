using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Playback tracking service.
/// </summary>
public interface IPlaybackTrackingService
{
    /// <summary>
    /// Add item (start tracking) of a specified item for a specified user.
    /// </summary>
    /// <param name="userId">ID of user to track the item for.</param>
    /// <param name="item">Item to track.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success.</returns>
    public Task<bool> AddItemAsync(string userId, Audio item, CancellationToken cancellationToken);

    /// <summary>
    /// Get tracked item for a specified item and user.
    /// </summary>
    /// <param name="userId">ID of user the item is being tracked for.</param>
    /// <param name="itemId">ID of the tracked item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tracked item, or null when not found.</returns>
    public Task<TrackedItem?> GetItemAsync(string userId, string itemId, CancellationToken cancellationToken);

    /// <summary>
    /// Remove specified tracked item for specified user.
    /// </summary>
    /// <param name="userId">ID of user the item is being tracked for.</param>
    /// <param name="item">Tracked item to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success.</returns>
    public Task<bool> RemoveItemAsync(string userId, TrackedItem item, CancellationToken cancellationToken);

    /// <summary>
    /// Invalidate specified item for specified user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="item">Item to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success.</returns>
    public Task<bool> InvalidateItemAsync(string userId, TrackedItem item, CancellationToken cancellationToken);
}
