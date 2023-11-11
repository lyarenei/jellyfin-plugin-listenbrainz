using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Api;

/// <summary>
/// ListenBrainz API client.
/// </summary>
public class ListenBrainzApiClient : IListenBrainzApiClient
{
    private readonly BaseClient _client;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzApiClient"/> class.
    /// </summary>
    /// <param name="client">Underlying base client.</param>
    /// <param name="logger">Logger instance.</param>
    public ListenBrainzApiClient(BaseClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ValidateTokenResponse?> ValidateToken(ValidateTokenRequest request, CancellationToken cancellationToken)
    {
        return await _client.Get<ValidateTokenRequest, ValidateTokenResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SubmitListensResponse?> SubmitListens(SubmitListensRequest request, CancellationToken cancellationToken)
    {
        return await _client.Post<SubmitListensRequest, SubmitListensResponse>(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RecordingFeedbackResponse?> SubmitRecordingFeedback(RecordingFeedbackRequest request, CancellationToken cancellationToken)
    {
        return await _client.Post<RecordingFeedbackRequest, RecordingFeedbackResponse>(request, cancellationToken);
    }
}
