using MediaBrowser.Controller.Library;

namespace ListenBrainzPlugin.Interfaces;

/// <summary>
/// Jellyfin playback watcher.
/// </summary>
public interface IJellyfinPlaybackWatcher
{
    /// <summary>
    /// Run this code when playback starts.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStart(object? sender, PlaybackProgressEventArgs args);

    /// <summary>
    /// Run this code when playback stops.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStop(object? sender, PlaybackStopEventArgs args);
}
