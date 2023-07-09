using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;

namespace Jellyfin.Plugin.Listenbrainz;

/// <summary>
/// Interface specifying a playback tracker plugin.
/// </summary>
public interface IPlaybackTrackerPlugin
{
    /// <summary>
    /// Code to run when <see cref="ISessionManager.PlaybackStart"/> event is emitted.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event arguments.</param>
    public void OnPlaybackStarted(object? sender, PlaybackProgressEventArgs args);

    /// <summary>
    /// Code to run when <see cref="ISessionManager.PlaybackStopped"/> event is emitted.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event arguments.</param>
    public void OnPlaybackStopped(object? sender, PlaybackStopEventArgs args);

    /// <summary>
    /// Code to run when <see cref="IUserDataManager.UserDataSaved"/> event is emitted.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event arguments.</param>
    public void OnUserDataSaved(object? sender, UserDataSaveEventArgs args);
}
