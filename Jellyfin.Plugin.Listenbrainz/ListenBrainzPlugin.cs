using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.Listenbrainz;

/// <summary>
/// ListenBrainz implementation of playback tracker plugin.
/// </summary>
public class ListenBrainzPlugin : IPlaybackTrackerPlugin
{
    /// <inheritdoc />
    public void OnPlaybackStarted(object? sender, PlaybackProgressEventArgs args)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void OnPlaybackStopped(object? sender, PlaybackStopEventArgs args)
    {
        throw new System.NotImplementedException();
    }

    /// <inheritdoc />
    public void OnUserDataSaved(object? sender, UserDataSaveEventArgs args)
    {
        throw new System.NotImplementedException();
    }
}
