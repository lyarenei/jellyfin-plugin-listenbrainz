using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// MusicBrainz client.
/// </summary>
public interface IMusicBrainzClient
{
    /// <summary>
    /// Get additional metadata for specified audio item.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <returns>Audio item metadata.</returns>
    public AudioItemMetadata GetAudioItemMetadata(BaseItem item);

    /// <summary>
    /// Get additional metadata for specified audio item.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audio item metadata.</returns>
    public Task<AudioItemMetadata> GetAudioItemMetadataAsync(BaseItem item, CancellationToken cancellationToken);
}
