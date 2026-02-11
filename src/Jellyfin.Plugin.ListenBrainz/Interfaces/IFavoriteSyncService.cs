namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Service for syncing favorites.
/// </summary>
public interface IFavoriteSyncService
{
    /// <summary>
    /// Gets a value indicating whether the service is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether the service is disabled.
    /// </summary>
    bool IsDisabled { get; }

    /// <summary>
    /// Sync a favorite Jellyfin track to a loved ListenBrainz recording.
    /// </summary>
    /// <param name="itemId">ID of the audio item.</param>
    /// <param name="jellyfinUserId">ID of the Jellyfin user.</param>
    /// <param name="listenTs">Listen timestamp. If specified, MSID sync will be attempted if recording MBID is not available.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sync has been successful.</returns>
    public Task<bool> SyncToListenBrainzAsync(Guid itemId, Guid jellyfinUserId, long? listenTs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables the service.
    /// </summary>
    public void Enable();

    /// <summary>
    /// Disables the service.
    /// </summary>
    public void Disable();
}
