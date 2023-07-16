using ListenBrainzPlugin.Dtos;
using ListenBrainzPlugin.Exceptions;
using ListenBrainzPlugin.Extensions;
using ListenBrainzPlugin.Interfaces;
using ListenBrainzPlugin.MusicBrainzApi.Interfaces;
using ListenBrainzPlugin.MusicBrainzApi.Models.Requests;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace ListenBrainzPlugin.Clients;

/// <summary>
/// MusicBrainz client for plugin.
/// </summary>
public class MusicBrainzClient : IMetadataClient
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
    public async Task<AudioItemMetadata> GetAudioItemMetadata(Audio item)
    {
        var trackMbid = item.GetTrackMbid();
        if (trackMbid is null) throw new ArgumentException("Audio item does not have a track MBID");

        var request = new RecordingRequest(trackMbid);
        var response = await _apiClient.GetRecording(request, CancellationToken.None);
        if (response is null) throw new MetadataException("No response received");
        if (!response.Recordings.Any()) throw new MetadataException("No metadata in response");
        return new AudioItemMetadata(response.Recordings.First());
    }
}
