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
    /// Get related recording MBIDs for a given recording MBID.
    /// </summary>
    /// <param name="recordingMbid">MusicBrainz recording ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of related recording MBIDs.</returns>
    public Task<IEnumerable<string>> GetRelatedRecordingMbidsAsync(string recordingMbid, CancellationToken cancellationToken);
}
