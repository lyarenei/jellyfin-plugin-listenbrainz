using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients;

/// <summary>
/// Musicbrainz API client.
/// </summary>
public class MusicbrainzClient : BaseMusicbrainzClient
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicbrainzClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public MusicbrainzClient(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService sleepService) : base(httpClientFactory, logger, sleepService)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get recording MBID by track MBID.
    /// </summary>
    /// <param name="trackId">ID of the track.</param>
    /// <returns>Recording MBID. Null if error or not found.</returns>
    public async Task<string?> GetRecordingId(string trackId)
    {
        _logger.LogDebug("Getting Recording MBID for Track: {TrackMbId}", trackId);
        var response = await Get<RecordingIdRequest, RecordingsResponse>(new RecordingIdRequest(trackId)).ConfigureAwait(false);
        if (response == null || response.IsError())
        {
            _logger.LogInformation("Failed to retrieve Recording ID for '{TrackMbId}'", trackId);
            return null;
        }

        var recording = response.Recordings.MaxBy(r => r.Score);
        if (recording != null)
        {
            return recording.Id;
        }

        _logger.LogInformation("Recording ID for track '{TrackMbId}' not found", trackId);
        return null;
    }
}
