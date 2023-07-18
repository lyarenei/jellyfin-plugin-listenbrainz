using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Cache for storing listens.
/// </summary>
public interface IListensCache
{
    /// <summary>
    /// Add specified listen to cache.
    /// </summary>
    /// <param name="userId">Jellyfin user ID associated with the listen.</param>
    /// <param name="item">Audio item associated with the listen.</param>
    /// <param name="metadata">Additional metadata for the item.</param>
    /// <param name="listenedAt">UNIX timestamp when the listens occured.</param>
    public void AddListen(Guid userId, Audio item, AudioItemMetadata? metadata, long listenedAt);

    /// <summary>
    /// Get all listens in cache for specified user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID associated with the listens.</param>
    /// <returns>Listens stored in cache.</returns>
    public IEnumerable<StoredListen> GetListens(Guid userId);

    /// <summary>
    /// Remove specified listens from cache for specified user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID associated with the listens.</param>
    /// <param name="listens">Listens to remove.</param>
    public void RemoveListens(Guid userId, IEnumerable<StoredListen> listens);
}
