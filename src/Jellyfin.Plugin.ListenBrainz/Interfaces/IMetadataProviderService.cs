using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Metadata provider interface.
/// </summary>
public interface IMetadataProviderService
{
    /// <summary>
    /// Get additional metadata for specified audio item.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Audio item metadata. Null on failure.</returns>
    public Task<AudioItemMetadata?> GetAudioItemMetadataAsync(Audio item, CancellationToken cancellationToken);
}
