using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Services.ListenCache;

/// <summary>
/// Default listen cache implementation.
/// </summary>
public class DefaultListenCache : IListenCache
{
    private readonly string _cachePath;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<DefaultListenCache> _logger;
    private Dictionary<string, List<Listen>> _listens;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultListenCache"/> class.
    /// </summary>
    /// <param name="cachePath">Path to the cache file.</param>
    /// <param name="logger">Logger instance.</param>
    public DefaultListenCache(string cachePath, ILogger<DefaultListenCache> logger)
    {
        _cachePath = cachePath;
        _listens = new Dictionary<string, List<Listen>>();
        _logger = logger;

        // Enable pretty-print to allow easy user editing
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };

        Task.Run(LoadFromFile);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultListenCache"/> class.
    /// </summary>
    /// <param name="cachePath">Path to the cache file.</param>
    /// <param name="cacheData">Initial cache data.</param>
    /// <param name="logger">Logger instance.</param>
    public DefaultListenCache(string cachePath, Dictionary<string, List<Listen>> cacheData, ILogger<DefaultListenCache> logger)
    {
        _cachePath = cachePath;
        _listens = cacheData;
        _logger = logger;

        // Enable pretty-print to allow easy user editing
        _serializerOptions = new JsonSerializerOptions { WriteIndented = true };
    }

    /// <inheritdoc />
    public async Task Save()
    {
        await using var stream = File.Create(_cachePath);
        await JsonSerializer.SerializeAsync(stream, _listens, _serializerOptions);
        await stream.DisposeAsync();

        _logger.LogDebug("Listen cache file has been updated");
    }

    /// <inheritdoc />
    public async Task LoadFromFile()
    {
        await using var stream = File.OpenRead(_cachePath);
        var data = await JsonSerializer.DeserializeAsync<Dictionary<string, List<Listen>>>(stream);
        if (data == null) return;

        _listens = data;
        _logger.LogDebug("Listen cache has been updated");
    }

    /// <inheritdoc />
    public void Add(LbUser user, Listen listen)
    {
        if (!_listens.ContainsKey(user.Name)) _listens.Add(user.Name, new List<Listen>());

        _listens[user.Name].Add(listen);
        _logger.LogInformation("Listen for user {User} has been saved to cache", user.Name);
    }

    /// <inheritdoc />
    public Collection<Listen> Get(LbUser user)
    {
        if (_listens.ContainsKey(user.Name)) return new Collection<Listen>(_listens[user.Name]);
        return new Collection<Listen>();
    }

    /// <inheritdoc />
    public void Remove(LbUser user, IEnumerable<Listen> listens)
    {
        if (_listens.ContainsKey(user.Name)) _listens[user.Name].RemoveAll(listens.Contains);
    }
}
