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
    private readonly string _cachePath;
    private readonly JsonSerializerOptions _serializerOptions;
    private Dictionary<string, List<Listen>> _listens;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultListenCache"/> class.
    /// </summary>
    /// <param name="cachePath">Path to the cache file.</param>
    public DefaultListenCache(string cachePath)
    {
        _cachePath = cachePath;
        _listens = new Dictionary<string, List<Listen>>();

        // Enable pretty-print to allow easy user editing
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };

        if (File.Exists(_cachePath))
        {
            RestoreCache();
        }
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

        // Enable pretty-print to allow easy user editing
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    /// <inheritdoc />
    public async void Save()
    {
        await using var stream = File.Create(_cachePath);
        await JsonSerializer.SerializeAsync(stream, _listens, _serializerOptions);
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

    private async void RestoreCache()
    {
        await using var stream = File.OpenRead(_cachePath);
        var data = await JsonSerializer.DeserializeAsync<Dictionary<string, List<Listen>>>(stream);
        if (data != null)
        {
            _listens = data;
        }
    }
}
