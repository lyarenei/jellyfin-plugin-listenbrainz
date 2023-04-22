using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Services.PlaybackTracker;

/// <summary>
/// Playback tracker service interface.
/// </summary>
public interface IPlaybackTrackerService
{
    /// <summary>
    /// Start tracking of a specified <see cref="Audio"/> item for specified <see cref="User"/>.
    /// </summary>
    /// <param name="audio">Item to track.</param>
    /// <param name="user">Start the item tracking for this user.</param>
    public void StartTracking(Audio audio, User user);

    /// <summary>
    /// Determine if a specified <see cref="Audio"/> item is tracked for a specified <see cref="User"/>.
    /// </summary>
    /// <param name="audio">Audio item to check.</param>
    /// <param name="user">Check the item tracking for this user.</param>
    /// <returns>True if this audio is tracked for user. False otherwise.</returns>
    public bool IsTracked(Audio audio, User user);

    /// <summary>
    /// Stops tracking (removes) specified <see cref="Audio"/> item for a specified <see cref="User"/>.
    /// </summary>
    /// <param name="audio">Item to stop tracking.</param>
    /// <param name="user">Stop the item tracking for this user.</param>
    public void StopTracking(Audio audio, User user);

    /// <summary>
    /// Get newest tracked item corresponding to specified audio and user.
    /// </summary>
    /// <param name="audio">Audio to match tracked item with.</param>
    /// <param name="user">User associated with the tracked item.</param>
    /// <returns>Newest tracked audio item. Null if not found.</returns>
    public TrackedAudio? GetItem(Audio audio, User user);
}
