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
}
