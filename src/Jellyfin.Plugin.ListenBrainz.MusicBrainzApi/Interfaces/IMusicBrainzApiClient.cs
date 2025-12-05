using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Responses;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;

/// <summary>
/// MusicBrainz API client interface.
/// </summary>
public interface IMusicBrainzApiClient
{
    /// <summary>
    /// Get recording MusicBrainz data.
    /// </summary>
    /// <param name="request">Recording request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recording response.</returns>
    public Task<RecordingResponse> GetRecordingAsync(RecordingRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get recording relations.
    /// </summary>
    /// <param name="request">Recording relations request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recording relations response.</returns>
    public Task<RecordingRelationsResponse> GetRecordingRelationsAsync(
        RecordingRelationsRequest request,
        CancellationToken cancellationToken);
}
