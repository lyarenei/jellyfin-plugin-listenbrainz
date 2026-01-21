using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of <see cref="IPlaybackTrackingService"/>.
/// </summary>
public sealed class DefaultPlaybackTrackingService : IPlaybackTrackingService, IDisposable
{
    private static DefaultPlaybackTrackingService? _instance;
    private readonly SemaphoreSlim _lock;
    private readonly Dictionary<string, List<TrackedItem>> _items;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPlaybackTrackingService"/> class.
    /// </summary>
    public DefaultPlaybackTrackingService()
    {
        _lock = new SemaphoreSlim(1, 1);
        _items = new Dictionary<string, List<TrackedItem>>();
        _isDisposed = false;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="DefaultPlaybackTrackingService"/> class.
    /// </summary>
    ~DefaultPlaybackTrackingService() => Dispose(false);

    /// <summary>
    /// Gets the default singleton instance.
    /// </summary>
    public static DefaultPlaybackTrackingService Instance
    {
        get => _instance ??= new DefaultPlaybackTrackingService();
    }

    /// <inheritdoc />
    public async Task<bool> AddItemAsync(string userId, Audio item, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var trackedItem = item.AsTrackedItem(userId);
            if (_items.TryGetValue(userId, out var userList))
            {
                userList.RemoveAll(i => i.ItemId == item.Id.ToString());
                userList.Add(trackedItem);
                return true;
            }

            _items[userId] = [trackedItem];
        }
        finally
        {
            _lock.Release();
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<TrackedItem?> GetItemAsync(
        string userId,
        string itemId,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _items.TryGetValue(userId, out var userList);
            return userList?.Find(v => v.ItemId == itemId);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveItemAsync(
        string userId,
        TrackedItem item,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _items.TryGetValue(userId, out var userList);
            userList?.Remove(item);
        }
        finally
        {
            _lock.Release();
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> InvalidateItemAsync(
        string userId,
        TrackedItem item,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _items.TryGetValue(userId, out var userList);
            var trackedItem = userList?.FirstOrDefault(i => i.ItemId == item.ItemId);
            if (trackedItem is null)
            {
                return false;
            }

            trackedItem.IsValid = false;
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose unmanaged (own) and optionally managed resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        // Dispose unmanaged resources here.

        if (disposing)
        {
            _lock.Dispose();
        }

        _isDisposed = true;
    }
}
