using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Services.PlaybackTracker;

/// <summary>
/// Concrete implementation of <see cref="IPlaybackTrackerService"/>.
/// </summary>
public class PlaybackTracker : IPlaybackTrackerService
{
    private Dictionary<User, Collection<TrackedAudio>> _trackedItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTracker"/> class.
    /// </summary>
    public PlaybackTracker()
    {
        _trackedItems = new Dictionary<User, Collection<TrackedAudio>>();
    }

    /// <inheritdoc />
    public void StartTracking(Audio audio, User user)
    {
        if (!_trackedItems.ContainsKey(user))
            _trackedItems.Add(user, new Collection<TrackedAudio>());

        var newItem = new TrackedAudio(audioItem: audio, user: user);
        _trackedItems[user].Add(newItem);
    }

    /// <inheritdoc />
    public bool IsTracked(Audio audio, User user)
    {
        if (!_trackedItems.ContainsKey(user)) { return false; }

        return _trackedItems[user].LastOrDefault(item => EqualPredicate(item, audio, user)) != null;
    }

    /// <inheritdoc />
    public void StopTracking(Audio audio, User user)
    {
        if (!_trackedItems.ContainsKey(user)) { return; }

        if (_trackedItems[user].Count == 0) { return; }

        var idx = _trackedItems[user].ToList().FindLastIndex(item => EqualPredicate(item, audio, user));
        if (idx >= 0) { _trackedItems[user].RemoveAt(idx); }
    }

    /// <inheritdoc />
    public TrackedAudio? GetItem(Audio audio, User user)
    {
        if (!_trackedItems.ContainsKey(user)) { return null; }

        return _trackedItems[user].LastOrDefault(item => EqualPredicate(item, audio, user));
    }

    private static bool EqualPredicate(TrackedAudio trackedItem, Audio audio, User user)
    {
        return trackedItem.AudioItem.Equals(audio) && trackedItem.User == user;
    }
}
