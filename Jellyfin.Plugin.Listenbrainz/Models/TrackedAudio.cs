using System;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Models;

/// <summary>
/// A representation of a tracked audio item.
/// </summary>
public class TrackedAudio
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrackedAudio"/> class.
    /// </summary>
    /// <param name="audioItem">Item to track.</param>
    /// <param name="user">User to associate this tracking with.</param>
    /// <param name="startedAt">Time of tracking start. Defaults to current time.</param>
    public TrackedAudio(Audio audioItem, User user, DateTime? startedAt = null)
    {
        AudioItem = audioItem;
        User = user;
        StartedAt = startedAt ?? DateTime.Now;
    }

    /// <summary>
    /// Gets tracked audio item.
    /// </summary>
    public Audio AudioItem { get; }

    /// <summary>
    /// Gets user associated with this tracking.
    /// </summary>
    public User User { get; }

    /// <summary>
    /// Gets date and time of tracking start.
    /// </summary>
    public DateTime StartedAt { get; }
}
