using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Configuration;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients.MusicBrainz;

/// <summary>
/// Implementation of <see cref="IMusicBrainzClient"/>.
/// </summary>
public class DefaultMusicBrainzClient : BaseMusicbrainzClient, IMusicBrainzClient
{
    private readonly ILogger _logger;
    private readonly GlobalConfiguration _globalConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMusicBrainzClient"/> class.
    /// </summary>
    /// <param name="baseUrl">API base URL.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public DefaultMusicBrainzClient(
        string baseUrl,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService sleepService) : base(baseUrl, httpClientFactory, logger, sleepService)
    {
        _globalConfig = Plugin.Instance?.Configuration.GlobalConfig ?? throw new InvalidOperationException("plugin configuration is NULL");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Recording?> GetRecordingData(string trackId)
    {
        _logger.LogDebug("Getting Recording data for Track: {TrackMbId}", trackId);
        if (!_globalConfig.MusicbrainzEnabled)
        {
            _logger.LogDebug("Nothing to do - Musicbrainz integration is disabled");
            return null;
        }

        var response = await Get<RecordingIdRequest, RecordingsResponse>(new RecordingIdRequest(trackId)).ConfigureAwait(true);
        if (response == null || response.IsError())
        {
            _logger.LogInformation("Failed to retrieve Recording data for '{TrackMbId}'", trackId);
            return null;
        }

        var recording = response.Recordings.OrderBy(r => r.Score).FirstOrDefault();
        if (recording != null)
        {
            return recording;
        }

        _logger.LogInformation("Recording data for track '{TrackMbId}' not found", trackId);
        return null;
    }
}
