using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Dtos;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients;

/// <summary>
/// Implementation of <see cref="IMusicBrainzClient"/>.
/// </summary>
public class MusicBrainzClient : IMusicBrainzClient
{
    private readonly ILogger _logger;
    private readonly IMusicBrainzApiClient _apiClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicBrainzClient"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="apiClient">MusicBrainz API client.</param>
    public MusicBrainzClient(ILogger logger, IMusicBrainzApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    /// <inheritdoc />
    public async Task<Recording?> GetRecordingByTrackId(string trackId)
    {
        _logger.LogDebug("Getting Recording data for track MBID {TrackMbid}", trackId);
        var request = new RecordingRequest(trackId);
        RecordingResponse? response;
        try
        {
            response = await _apiClient.GetRecording(request, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(
                "Failed to retrieve recording data for track MBID {TrackMbid}: {Message}",
                trackId,
                ex.Message);
            _logger.LogDebug(ex, "Failed to retrieve recording data");
            return null;
        }

        if (response is not null && response.Recordings.Any()) return response.Recordings.MaxBy(r => r.Score);

        _logger.LogInformation("No recordings have been retrieved for track MBID {TrackMbid}", trackId);
        return null;
    }
}
