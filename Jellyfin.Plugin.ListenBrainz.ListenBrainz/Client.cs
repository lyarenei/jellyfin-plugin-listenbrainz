using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz;

/// <summary>
/// ListenBrainz API client.
/// </summary>
public class Client : BaseClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="baseUrl">API base URL.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public Client(string baseUrl, IHttpClientFactory httpClientFactory, ILogger logger, ISleepService sleepService)
        : base(baseUrl, httpClientFactory, logger, sleepService)
    {
    }

    /// <summary>
    /// Validate provided token.
    /// </summary>
    /// <param name="request">Validate token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public async Task<ValidateTokenResponse?> ValidateToken(ValidateTokenRequest request, CancellationToken cancellationToken)
    {
        return await Get<ValidateTokenRequest, ValidateTokenResponse>(request, cancellationToken);
    }

    /// <summary>
    /// Submit listens.
    /// </summary>
    /// <param name="request">Submit listens request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public async Task<SubmitListensResponse?> SubmitListens(SubmitListensRequest request, CancellationToken cancellationToken)
    {
        return await Post<SubmitListensRequest, SubmitListensResponse>(request, cancellationToken);
    }
}
