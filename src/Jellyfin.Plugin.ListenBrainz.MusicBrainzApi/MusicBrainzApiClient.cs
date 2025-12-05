using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;

/// <summary>
/// MusicBrainz API client.
/// </summary>
public class MusicBrainzApiClient : BaseClient, IMusicBrainzApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MusicBrainzApiClient"/> class.
    /// </summary>
    /// <param name="clientName">Name of the client application.</param>
    /// <param name="clientVersion">Version of the client application.</param>
    /// <param name="contactUrl">Where the maintainer can be contacted.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public MusicBrainzApiClient(
        string clientName,
        string clientVersion,
        string contactUrl,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService? sleepService = null)
        : base(clientName, clientVersion, contactUrl, httpClientFactory, logger, sleepService)
    {
    }

    /// <inheritdoc />
    public async Task<RecordingResponse> GetRecordingAsync(RecordingRequest request, CancellationToken cancellationToken)
    {
        return await Get<RecordingRequest, RecordingResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RecordingRelationsResponse> GetRecordingRelationsAsync(RecordingRelationsRequest request, CancellationToken cancellationToken)
    {
        return await Get<RecordingRelationsRequest, RecordingRelationsResponse>(request, cancellationToken);
    }
}
