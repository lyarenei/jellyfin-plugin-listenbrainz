using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients
{
    /// <summary>
    /// Dummy implementation of <see cref="IMusicbrainzClientService"/>.
    /// </summary>
    public class DummyMusicbrainzClient : IMusicbrainzClientService
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DummyMusicbrainzClient"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public DummyMusicbrainzClient(ILogger logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<Recording?> GetRecordingData(string trackId)
        {
            _logger.LogDebug("Using dummy implementation of Musicbrainz client - no recording data available");
            return new Task<Recording?>(() => null);
        }
    }
}
