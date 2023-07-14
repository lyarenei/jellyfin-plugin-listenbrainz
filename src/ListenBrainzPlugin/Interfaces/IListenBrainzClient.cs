using ListenBrainzPlugin.Configuration;
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
    public void SendNowPlaying(ListenBrainzUserConfig config, Audio item);
}
