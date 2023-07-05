using System.Collections.ObjectModel;
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
    public void Save();

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
}