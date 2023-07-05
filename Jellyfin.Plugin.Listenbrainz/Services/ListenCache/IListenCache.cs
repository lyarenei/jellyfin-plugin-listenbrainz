using System.IO;
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
    /// <param name="listen">Listen to add.</param>
    public void Add(Listen listen);
}
