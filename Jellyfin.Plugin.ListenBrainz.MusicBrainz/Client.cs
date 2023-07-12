using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainz;

/// <summary>
/// MusicBrainz API client.
/// </summary>
public class Client : BaseClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="baseUrl">API base URL.</param>
    /// <param name="clientName">Name of the client application.</param>
    /// <param name="clientVersion">Version of the client application.</param>
    /// <param name="clientLink">Link to additional info about the client application.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    protected Client(
        string baseUrl,
        string clientName,
        string clientVersion,
        string clientLink,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService sleepService)
        : base(baseUrl, clientName, clientVersion, clientLink, httpClientFactory, logger, sleepService)
    {
    }

    /// <summary>
    /// Get recording MusicBrainz data.
    /// </summary>
    /// <param name="request">Recording request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recording response.</returns>
    public async Task<RecordingResponse?> GetRecording(RecordingRequest request, CancellationToken cancellationToken)
    {
        return await Get<RecordingRequest, RecordingResponse>(request, cancellationToken);
    }
}
