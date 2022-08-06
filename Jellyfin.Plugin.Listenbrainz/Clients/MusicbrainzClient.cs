using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients
{
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
        /// Get recording data by track MBID.
        /// </summary>
        /// <param name="trackId">ID of the track.</param>
        /// <returns>An instance of <see cref="Recording"/>. Null if error or not found.</returns>
        public async Task<Recording?> GetRecordingData(string trackId)
        {
            _logger.LogDebug("Getting Recording data for Track: {TrackMbId}", trackId);
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
}
