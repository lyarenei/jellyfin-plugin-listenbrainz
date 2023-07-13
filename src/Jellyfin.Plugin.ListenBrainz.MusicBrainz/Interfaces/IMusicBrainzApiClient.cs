using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Responses;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainz.Interfaces;

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
