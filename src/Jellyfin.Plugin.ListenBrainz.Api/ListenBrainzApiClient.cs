using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Api;

/// <summary>
/// ListenBrainz API client.
/// </summary>
public class ListenBrainzApiClient : BaseClient, IListenBrainzApiClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzApiClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public ListenBrainzApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService? sleepService = null)
        : base(httpClientFactory, logger, sleepService)
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

    /// <inheritdoc />
    public async Task<RecordingFeedbackResponse?> SubmitRecordingFeedback(RecordingFeedbackRequest request, CancellationToken cancellationToken)
    {
        return await Post<RecordingFeedbackRequest, RecordingFeedbackResponse>(request, cancellationToken);
    }
}
