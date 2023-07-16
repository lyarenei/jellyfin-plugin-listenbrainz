using ListenBrainzPlugin.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace ListenBrainzPlugin.Interfaces;

/// <summary>
/// MusicBrainz client.
/// </summary>
public interface IMetadataClient
{
    /// <summary>
    /// Get additional metadata from MusicBrainz for specified audio item.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <returns>MusicBrainz recording.</returns>
    public Task<AudioItemMetadata> GetAudioItemMetadata(Audio item);
}
