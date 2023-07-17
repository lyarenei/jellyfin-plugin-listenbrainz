using System.Text.Json;
using ListenBrainzPlugin.Dtos;
using ListenBrainzPlugin.Exceptions;
using ListenBrainzPlugin.Extensions;
using ListenBrainzPlugin.Interfaces;
using MediaBrowser.Controller.Entities.Audio;

namespace ListenBrainzPlugin.Managers;

/// <summary>
/// Cache manager.
/// </summary>
public class CacheManager : ICacheManager, IListensCache
{
    /// <summary>
    /// Cache file name.
    /// </summary>
    public const string CacheFileName = "cache.json";

    /// <summary>
    /// JSON serializer options.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _cachePath;
    private readonly object _lock = new();
    private static CacheManager? _instance;
    private Dictionary<Guid, List<StoredListen>> _listensCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheManager"/> class.
    /// </summary>
    /// <param name="cacheFilePath">Path to the cache file.</param>
    private CacheManager(string cacheFilePath)
    {
        _cachePath = cacheFilePath;
        _listensCache = new Dictionary<Guid, List<StoredListen>>();

        if (File.Exists(_cachePath)) Restore();
    }

    /// <summary>
    /// Gets instance of the cache manager.
    /// </summary>
    public static CacheManager Instance
    {
        get
        {
            if (_instance is null) _instance = new CacheManager(Path.Join(Plugin.GetDataPath(), CacheFileName));
            return _instance;
        }
    }

    /// <inheritdoc />
    public void Save()
    {
        try
        {
            Monitor.Enter(_lock);
            using var stream = File.Create(_cachePath);
            JsonSerializer.Serialize(stream, _listensCache, SerializerOptions);
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <inheritdoc />
    public void Restore()
    {
        try
        {
            Monitor.Enter(_lock);
            using var stream = File.OpenRead(_cachePath);
            var data = JsonSerializer.Deserialize<Dictionary<Guid, List<StoredListen>>>(stream, SerializerOptions);
            _listensCache = data ?? throw new ListenBrainzPluginException("Deserialized cache file to null");
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <inheritdoc />
    public void AddListen(Guid userId, Audio item, AudioItemMetadata? metadata, long listenedAt)
    {
        try
        {
            Monitor.Enter(_lock);
            if (!_listensCache.ContainsKey(userId)) _listensCache.Add(userId, new List<StoredListen>());
            _listensCache[userId].Add(item.AsStoredListen(listenedAt, metadata));
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <inheritdoc />
    public IEnumerable<StoredListen> GetListens(Guid userId)
    {
        return _listensCache.ContainsKey(userId) ? _listensCache[userId] : Array.Empty<StoredListen>();
    }

    /// <inheritdoc />
    public void RemoveListens(Guid userId, IEnumerable<StoredListen> listens)
    {
        try
        {
            Monitor.Enter(_lock);
            if (_listensCache.ContainsKey(userId)) _listensCache[userId].RemoveAll(listens.Contains);
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }
}
