using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Services;

using ListenCacheData = System.Collections.Generic.Dictionary<
    System.Guid,
    System.Collections.Generic.List<
        Jellyfin.Plugin.ListenBrainz.Dtos.StoredListen
    >
>;

/// <summary>
/// Default implementation of <see cref="IListensCachingService"/>.
/// </summary>
public sealed class DefaultListensCachingService : IListensCachingService, IDisposable
{
    private static DefaultListensCachingService? _instance;

    private readonly SemaphoreSlim _lock;
    private readonly IPersistentJsonService<ListenCacheData> _persistentCache;
    private readonly ListenCacheData _listensCache;
    private readonly ILogger _logger;

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultListensCachingService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="persistentCache">Persistent cache service.</param>
    /// <param name="restore">Restore data from file.</param>
    public DefaultListensCachingService(
        ILogger logger,
        IPersistentJsonService<ListenCacheData> persistentCache,
        bool restore = true)
    {
        _listensCache = new ListenCacheData();
        _lock = new SemaphoreSlim(1, 1);
        _isDisposed = false;
        _persistentCache = persistentCache;
        _logger = logger;
        _instance = this;

        if (!restore)
        {
            return;
        }

        try
        {
            _listensCache = _persistentCache.Read();
        }
        catch (ServiceException ex)
        {
            _logger.LogDebug(ex, "Failed to restore listens cache from persistent storage");
        }
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="DefaultListensCachingService"/> class.
    /// </summary>
    ~DefaultListensCachingService() => Dispose(false);

    /// <summary>
    /// Gets instance of the cache manager.
    /// </summary>
    /// <returns>Singleton instance of <see cref="DefaultFavoriteSyncService"/>.</returns>
    public static DefaultListensCachingService GetInstance()
    {
        if (_instance is null)
        {
            throw new ServiceException("Service is not initialized");
        }

        return _instance;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose managed and unmanaged (own) resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _lock.Dispose();
            if (_persistentCache is IDisposable disposableCache)
            {
                disposableCache.Dispose();
            }
        }

        _isDisposed = true;
    }

    /// <inheritdoc />
    public void AddListen(Guid userId, Audio item, AudioItemMetadata? metadata, long listenedAt)
    {
        _lock.Wait();

        try
        {
            var storedListen = item.AsStoredListen(listenedAt, metadata);
            if (_listensCache.TryGetValue(userId, out var userListens))
            {
                userListens.Add(storedListen);
                return;
            }

            _listensCache[userId] = [storedListen];
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AddListenAsync(Guid userId, Audio item, AudioItemMetadata? metadata, long listenedAt)
    {
        await _lock.WaitAsync();

        try
        {
            var storedListen = item.AsStoredListen(listenedAt, metadata);
            if (_listensCache.TryGetValue(userId, out var userListens))
            {
                userListens.Add(storedListen);
                return;
            }

            _listensCache[userId] = [storedListen];
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public IEnumerable<StoredListen> GetListens(Guid userId)
    {
        _lock.Wait();

        try
        {
            _listensCache.TryGetValue(userId, out var userListens);
            return userListens?.ToList() ?? Enumerable.Empty<StoredListen>();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void RemoveListen(Guid userId, StoredListen listen)
    {
        _lock.Wait();

        try
        {
            _listensCache.TryGetValue(userId, out var userListens);
            userListens?.RemoveAll(l => l.Id == listen.Id && l.ListenedAt == listen.ListenedAt);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void RemoveListens(Guid userId, IEnumerable<StoredListen> listens)
    {
        _lock.Wait();
        var storedListens = listens.Select(sl => (sl.Id, sl.ListenedAt)).ToHashSet();

        try
        {
            _listensCache.TryGetValue(userId, out var userListens);
            userListens?.RemoveAll(sl => storedListens.Contains((sl.Id, sl.ListenedAt)));
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task RemoveListensAsync(Guid userId, IEnumerable<StoredListen> listens)
    {
        await _lock.WaitAsync();
        var storedListens = listens.Select(sl => (sl.Id, sl.ListenedAt)).ToHashSet();

        try
        {
            _listensCache.TryGetValue(userId, out var userListens);
            userListens?.RemoveAll(sl => storedListens.Contains((sl.Id, sl.ListenedAt)));
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync()
    {
        await _lock.WaitAsync();
        try
        {
            await _persistentCache.SaveAsync(_listensCache);
        }
        finally
        {
            _lock.Release();
        }
    }
}
