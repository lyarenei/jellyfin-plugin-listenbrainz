using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Services.ListenCache;

/// <summary>
/// Listen cache interface.
/// </summary>
public interface IListenCache
{
    /// <summary>
    /// Persist cache to disk.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Save();

    /// <summary>
    /// Load persisted data from cache file.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task LoadFromFile();

    /// <summary>
    /// Add a listen to a cache.
    /// </summary>
    /// <param name="user">User of the listen.</param>
    /// <param name="listen">Listen to add.</param>
    public void Add(LbUser user, Listen listen);

    /// <summary>
    /// Get listens for specified user.
    /// </summary>
    /// <param name="user">User of the listens.</param>
    /// <returns>Collection of listens.</returns>
    public Collection<Listen> Get(LbUser user);

    /// <summary>
    /// Remove specified listens from cache.
    /// </summary>
    /// <param name="user">User of the listens.</param>
    /// <param name="listens">Listens to remove.</param>
    public void Remove(LbUser user, IEnumerable<Listen> listens);
}
