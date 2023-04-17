using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Services.PlaybackTracker;

/// <summary>
/// Concrete implementation of <see cref="IPlaybackTrackerService"/>.
/// </summary>
public class PlaybackTracker : IPlaybackTrackerService
{
    private Dictionary<User, Collection<TrackedAudio>> _trackedItems;
    private ILogger<PlaybackTracker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackTracker"/> class.
    /// </summary>
    public PlaybackTracker(ILoggerFactory loggerFactory)
    {
        _trackedItems = new Dictionary<User, Collection<TrackedAudio>>();
        _logger = loggerFactory.CreateLogger<PlaybackTracker>();
    }

    /// <inheritdoc />
    public void StartTracking(Audio audio, User user)
    {
        if (!_trackedItems.ContainsKey(user))
            _trackedItems.Add(user, new Collection<TrackedAudio>());

        var newItem = new TrackedAudio(audioItem: audio, user: user);
        _trackedItems[user].Add(newItem);
        _logger.LogDebug(
            "Started tracking playback of {Item} for user {User}",
            audio.Id,
            user.Username);
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
        if (idx < 0) { return; }

        _trackedItems[user].RemoveAt(idx);
        _logger.LogDebug(
            "Stopped tracking playback of {Item} for user {User}",
            audio.Id,
            user.Username);
    }

    /// <inheritdoc />
    public TrackedAudio? GetItem(Audio audio, User user)
    {
        if (_trackedItems.ContainsKey(user))
            return _trackedItems[user].LastOrDefault(item => EqualPredicate(item, audio, user));

        return null;
    }

    private static bool EqualPredicate(TrackedAudio trackedItem, Audio audio, User user)
    {
        return trackedItem.AudioItem.Equals(audio) && trackedItem.User == user;
    }
}
