using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Models.Responses;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Interfaces;

/// <summary>
/// ListenBrainz API client.
/// </summary>
public interface IListenBrainzApiClient
{
    /// <summary>
    /// Validate provided token.
    /// </summary>
    /// <param name="request">Validate token request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public Task<ValidateTokenResponse?> ValidateToken(ValidateTokenRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Submit listens.
    /// </summary>
    /// <param name="request">Submit listens request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public Task<SubmitListensResponse?> SubmitListens(SubmitListensRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Submit a recording feedback.
    /// </summary>
    /// <param name="request">Recording feedback request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public Task<RecordingFeedbackResponse?> SubmitRecordingFeedback(RecordingFeedbackRequest request, CancellationToken cancellationToken);
}
