using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients.MusicBrainz
{
    /// <summary>
    /// Dummy implementation of <see cref="IMusicBrainzClient"/>.
    /// </summary>
    public class DummyMusicBrainzClient : IMusicBrainzClient
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DummyMusicBrainzClient"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public DummyMusicBrainzClient(ILogger logger)
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
