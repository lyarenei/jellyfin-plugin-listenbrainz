using ListenBrainzPlugin.Http.Interfaces;
using ListenBrainzPlugin.ListenBrainzApi.Interfaces;
using ListenBrainzPlugin.ListenBrainzApi.Models.Requests;
using ListenBrainzPlugin.ListenBrainzApi.Models.Responses;
using Microsoft.Extensions.Logging;

namespace ListenBrainzPlugin.ListenBrainzApi;

/// <summary>
/// ListenBrainz API client.
/// </summary>
public class ListenBrainzApiClient : BaseClient, IListenBrainzApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzApiClient"/> class.
    /// </summary>
    /// <param name="baseUrl">API base URL.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public ListenBrainzApiClient(
        string baseUrl,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService? sleepService = null)
        : base(baseUrl, httpClientFactory, logger, sleepService)
    {
    }

    /// <inheritdoc />
    public async Task<ValidateTokenResponse?> ValidateToken(ValidateTokenRequest request, CancellationToken cancellationToken)
    {
        return await Get<ValidateTokenRequest, ValidateTokenResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubmitListensResponse?> SubmitListens(SubmitListensRequest request, CancellationToken cancellationToken)
    {
        return await Post<SubmitListensRequest, SubmitListensResponse>(request, cancellationToken);
    }
}
