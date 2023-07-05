using System.Collections.Generic;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Services.ListenCache;

/// <summary>
/// Default listen cache implementation.
/// </summary>
public class DefaultListenCache : IListenCache
{
    private string _cachePath;
    private Dictionary<string, List<Listen>> _listens;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultListenCache"/> class.
    /// </summary>
    /// <param name="cachePath">Path to the cache file.</param>
    public DefaultListenCache(string cachePath)
    {
        _cachePath = cachePath;
        // TODO: load json from path
        _listens = new Dictionary<string, List<Listen>>();
    }

    /// <inheritdoc />
    public void Save()
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void Add(LbUser user, Listen listen)
    {
        throw new System.NotImplementedException();
    }
}
