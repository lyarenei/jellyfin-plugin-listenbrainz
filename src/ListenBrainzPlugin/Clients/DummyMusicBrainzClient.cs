using ListenBrainzPlugin.Dtos;
using ListenBrainzPlugin.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace ListenBrainzPlugin.Clients;

/// <summary>
/// Dummy implementation of MusicBrainz client.
/// Does nothing.
/// </summary>
public class DummyMusicBrainzClient : IMetadataClient
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
    public Task<AudioItemMetadata> GetAudioItemMetadata(Audio item)
    {
        throw new InvalidOperationException("MusicBrainz integration is disabled");
    }
}
