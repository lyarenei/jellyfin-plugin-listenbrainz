using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Dtos;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients;

/// <summary>
/// Dummy implementation of <see cref="IMusicBrainzClient"/>.
/// </summary>
public class DummyMusicbrainzClient : IMusicBrainzClient
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
    public Task<Recording?> GetRecordingByTrackId(string trackId)
    {
        _logger.LogDebug("Integration with MusicBrainz is not enabled; no recording data available");
        return Task.FromResult<Recording?>(null);
    }
}
