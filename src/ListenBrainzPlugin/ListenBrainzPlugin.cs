using ListenBrainzPlugin.Interfaces;
using MediaBrowser.Controller.Library;

namespace ListenBrainzPlugin;

/// <summary>
/// ListenBrainz plugin implementation.
/// </summary>
public class ListenBrainzPlugin : IJellyfinPlaybackWatcher
{
    /// <summary>
    /// Sends 'playing now' listen to ListenBrainz.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStart(object? sender, PlaybackProgressEventArgs args)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Sends listen of track to ListenBrainz if conditions have been met.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStop(object? sender, PlaybackStopEventArgs args)
    {
        throw new NotImplementedException();
    }
}
