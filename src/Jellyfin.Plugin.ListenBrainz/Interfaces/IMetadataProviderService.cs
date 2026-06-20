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

    /// <summary>
    /// Get recording MBIDs related to the specified recording.
    /// </summary>
    /// <param name="recordingMbid">MusicBrainz ID identifying the recording.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of related recording MBIDs.</returns>
    public Task<IEnumerable<string>> GetRelatedRecordingMbidsAsync(string recordingMbid, CancellationToken cancellationToken);
}
