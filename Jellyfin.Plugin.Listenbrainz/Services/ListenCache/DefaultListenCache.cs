using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultListenCache"/> class.
    /// </summary>
    /// <param name="cachePath">Path to the cache file.</param>
    /// <param name="cacheData">Initial cache data.</param>
    public DefaultListenCache(string cachePath, Dictionary<string, List<Listen>> cacheData)
    {
        _cachePath = cachePath;
        _listens = cacheData;
    }

    /// <inheritdoc />
    public async void Save()
    {
        await using var stream = File.Create(_cachePath);
        await JsonSerializer.SerializeAsync(stream, _listens);
        await stream.DisposeAsync();
    }

    /// <inheritdoc />
    public void Add(LbUser user, Listen listen)
    {
        if (!_listens.ContainsKey(user.Name))
        {
            _listens.Add(user.Name, new List<Listen>());
        }

        _listens[user.Name].Add(listen);
    }

    /// <inheritdoc />
    public Collection<Listen> Get(LbUser user)
    {
        if (!_listens.ContainsKey(user.Name))
        {
            return new Collection<Listen>();
        }

        return new Collection<Listen>(_listens[user.Name]);
    }
}
