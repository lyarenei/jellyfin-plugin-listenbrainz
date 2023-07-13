using ListenBrainzPlugin.MusicBrainzApi.Models.Requests;
using ListenBrainzPlugin.MusicBrainzApi.Models.Responses;

namespace ListenBrainzPlugin.MusicBrainzApi.Interfaces;

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
    public Task<RecordingResponse?> GetRecording(RecordingRequest request, CancellationToken cancellationToken);
}
