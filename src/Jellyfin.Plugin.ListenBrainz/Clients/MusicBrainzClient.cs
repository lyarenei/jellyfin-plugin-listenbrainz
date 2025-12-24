using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Requests;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Clients;

/// <summary>
/// MusicBrainz client for plugin.
/// </summary>
[Obsolete("Use IMetadataProviderService instead")]
public class MusicBrainzClient : IMusicBrainzClient
{
    private readonly ILogger _logger;
    private readonly IMusicBrainzApiClient _apiClient;
    private readonly IPluginConfigService _pluginConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="MusicBrainzClient"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="apiClient">MusicBrainz API client.</param>
    /// <param name="pluginConfig">Plugin configuration service.</param>
    public MusicBrainzClient(ILogger logger, IMusicBrainzApiClient apiClient, IPluginConfigService pluginConfig)
    {
        _logger = logger;
        _apiClient = apiClient;
        _pluginConfig = pluginConfig;
    }

    /// <inheritdoc />
    /// <exception cref="AggregateException">Getting metadata failed.</exception>
    /// <exception cref="ArgumentException">Invalid audio item data.</exception>
    /// <exception cref="MetadataException">Metadata not available.</exception>
    public AudioItemMetadata GetAudioItemMetadata(BaseItem item)
    {
        var trackMbid = item.GetTrackMbid();
        if (trackMbid is null)
        {
            throw new ArgumentException("Audio item does not have a track MBID");
        }

        var request = new RecordingRequest(trackMbid) { BaseUrl = _pluginConfig.MusicBrainzApiUrl };
        var task = _apiClient.GetRecordingAsync(request, CancellationToken.None);
        task.Wait();
        if (task.Exception is not null)
        {
            throw task.Exception;
        }

        if (task.Result is null)
        {
            throw new MetadataException("No response received");
        }

        if (!task.Result.Recordings.Any())
        {
            throw new MetadataException("No metadata in response");
        }

        return new AudioItemMetadata(task.Result.Recordings.First());
    }

    /// <inheritdoc />
    public async Task<AudioItemMetadata> GetAudioItemMetadataAsync(BaseItem item, CancellationToken cancellationToken)
    {
        var trackMbid = item.GetTrackMbid();
        if (trackMbid is null)
        {
            throw new ArgumentException("Audio item does not have a track MBID");
        }

        var request = new RecordingRequest(trackMbid) { BaseUrl = _pluginConfig.MusicBrainzApiUrl };
        var resp = await _apiClient.GetRecordingAsync(request, cancellationToken);
        return new AudioItemMetadata(resp.Recordings.First());
    }
}
