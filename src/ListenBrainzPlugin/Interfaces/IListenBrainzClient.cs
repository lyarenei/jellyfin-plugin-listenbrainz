using ListenBrainzPlugin.Configuration;
using ListenBrainzPlugin.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace ListenBrainzPlugin.Interfaces;

/// <summary>
/// ListenBrainz client.
/// </summary>
public interface IListenBrainzClient
{
    /// <summary>
    /// Send 'now playing' listen of specified item.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="item">Audio item currently being listened to.</param>
    /// <param name="audioMetadata">Additional metadata for this audio item.</param>
    public void SendNowPlaying(ListenBrainzUserConfig config, Audio item, AudioItemMetadata? audioMetadata);

    /// <summary>
    /// Send a single listen of specified item.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="item">Audio item of the listen.</param>
    /// <param name="metadata">Additional metadata for this audio item.</param>
    public void SendListen(ListenBrainzUserConfig config, Audio item, AudioItemMetadata? metadata);

    /// <summary>
    /// Send a feedback for a specific recording, identified by either a MBID or MSID.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="isFavorite">The recording is marked as favorite.</param>
    /// <param name="recordingMbid">MusicBrainz ID identifying the recording.</param>
    /// <param name="recordingMsid">MessyBrainz ID identifying the recording.</param>
    public void SendFeedback(ListenBrainzUserConfig config, bool isFavorite, string? recordingMbid = null, string? recordingMsid = null);
}
