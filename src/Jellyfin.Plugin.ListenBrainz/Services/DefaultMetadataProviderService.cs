using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Requests;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of metadata provider service.
/// </summary>
public class DefaultMetadataProviderService : IMetadataProviderService
{
    private readonly ILogger _logger;
    private readonly IMusicBrainzApiClient _apiClient;
    private readonly IPluginConfigService _pluginConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMetadataProviderService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="apiClient">MusicBrainz API client.</param>
    /// <param name="pluginConfig">Plugin config service.</param>
    public DefaultMetadataProviderService(
        ILogger logger,
        IMusicBrainzApiClient apiClient,
        IPluginConfigService pluginConfig)
    {
        _logger = logger;
        _apiClient = apiClient;
        _pluginConfig = pluginConfig;
    }

    /// <inheritdoc />
    public async Task<AudioItemMetadata?> GetAudioItemMetadataAsync(Audio item, CancellationToken cancellationToken)
    {
        if (!_pluginConfig.IsMusicBrainzEnabled)
        {
            _logger.LogDebug("MusicBrainz integration is disabled, skipping metadata fetch");
            return null;
        }

        var trackMbid = item.GetTrackMbid();
        if (trackMbid is null)
        {
            _logger.LogDebug("Item does not have track MBID, cannot fetch metadata");
            return null;
        }

        try
        {
            var request = new RecordingRequest(trackMbid) { BaseUrl = _pluginConfig.MusicBrainzApiUrl };
            var resp = await _apiClient.GetRecordingAsync(request, cancellationToken);
            return new AudioItemMetadata(resp.Recordings.First());
        }
        catch (Exception e)
        {
            _logger.LogDebug("Could not get MusicBrainz metadata: {Message}", e.Message);
            _logger.LogTrace(e, "Exception occurred while getting MusicBrainz metadata");
            return null;
        }
    }
}
