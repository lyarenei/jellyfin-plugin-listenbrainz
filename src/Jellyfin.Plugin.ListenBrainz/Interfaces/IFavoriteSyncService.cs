namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Service for syncing favorites.
/// </summary>
public interface IFavoriteSyncService
{
    /// <summary>
    /// Syncs a favorite Jellyfin track to a loved ListenBrainz recording.
    /// </summary>
    /// <param name="itemId">ID of the audio item.</param>
    /// <param name="jellyfinUserId">ID of the Jellyfin user.</param>
    public void SyncToListenBrainz(Guid itemId, Guid jellyfinUserId);
}
