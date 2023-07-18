using ListenBrainzPlugin.Dtos;
using ListenBrainzPlugin.Extensions;
using MediaBrowser.Controller.Entities.Audio;

namespace ListenBrainzPlugin.Managers;

/// <summary>
/// Playback tracking manager.
/// </summary>
public class PlaybackTrackingManager
{
    private readonly object _lock = new();
    private static PlaybackTrackingManager? _instance;
    private Dictionary<string, List<TrackedItem>> _items;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTrackingManager"/> class.
    /// </summary>
    public PlaybackTrackingManager()
    {
        _items = new Dictionary<string, List<TrackedItem>>();
    }

    /// <summary>
    /// Gets instance of the cache manager.
    /// </summary>
    public static PlaybackTrackingManager Instance
    {
        get
        {
            if (_instance is null) _instance = new PlaybackTrackingManager();
            return _instance;
        }
    }

    /// <summary>
    /// Add item (start tracking) of a specified item for a specified user.
    /// </summary>
    /// <param name="userId">ID of user to track the item for.</param>
    /// <param name="item">Item to track.</param>
    public void AddItem(string userId, Audio item)
    {
        try
        {
            Monitor.Enter(_lock);
            if (!_items.ContainsKey(userId)) _items.Add(userId, new List<TrackedItem>());
            if (_items[userId].Exists(i => i.ItemId == item.Id.ToString()))
            {
                _items[userId].RemoveAll(i => i.ItemId == item.Id.ToString());
            }

            _items[userId].Add(item.AsTrackedItem(userId));
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <summary>
    /// Get tracked item of a specified item and user.
    /// </summary>
    /// <param name="userId">ID of user the item is being tracked for.</param>
    /// <param name="itemId">ID of the tracked item.</param>
    /// <returns>Tracked item. Null if not found.</returns>
    public TrackedItem? GetItem(string userId, string itemId)
    {
        try
        {
            Monitor.Enter(_lock);
            if (!_items.ContainsKey(userId)) return null;
            return _items[userId].Find(v => v.ItemId == itemId);
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <summary>
    /// Remove specified tracked item for specified user.
    /// </summary>
    /// <param name="userId">ID of user the item is being tracked for.</param>
    /// <param name="item">Tracked item to remove.</param>
    public void RemoveItem(string userId, TrackedItem item)
    {
        try
        {
            Monitor.Enter(_lock);
            if (!_items.ContainsKey(userId)) return;
            _items[userId].Remove(item);
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }

    /// <summary>
    /// Invalidate specified item for specified user.
    /// </summary>
    /// <param name="userId">Jellyfin user ID.</param>
    /// <param name="item">Item to invalidate.</param>
    public void InvalidateItem(string userId, TrackedItem item)
    {
        try
        {
            Monitor.Enter(_lock);
            if (!_items.ContainsKey(userId)) return;
            _items[userId].First(i => i.ItemId == item.ItemId).IsValid = false;
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }
}
