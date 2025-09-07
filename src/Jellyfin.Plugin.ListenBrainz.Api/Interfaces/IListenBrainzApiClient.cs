using Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

namespace Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

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
    public Task<ValidateTokenResponse> ValidateToken(ValidateTokenRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Submit listens.
    /// </summary>
    /// <param name="request">Submit listens request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public Task<SubmitListensResponse> SubmitListens(SubmitListensRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Submit a recording feedback.
    /// </summary>
    /// <param name="request">Recording feedback request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public Task<RecordingFeedbackResponse> SubmitRecordingFeedback(RecordingFeedbackRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get listens of a specified user.
    /// </summary>
    /// <param name="request">User listens request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public Task<GetUserListensResponse> GetUserListens(GetUserListensRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get listen(s) feedback of a specified user.
    /// </summary>
    /// <param name="request">User feedback request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public Task<GetUserFeedbackResponse> GetUserFeedback(GetUserFeedbackRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Get collaborator playlists of a specified user.
    /// </summary>
    /// <param name="request">Collaborator playlists request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    public Task<GetCollaboratorPlaylistsResponse> GetCollaboratorPlaylists(GetCollaboratorPlaylistsRequest request, CancellationToken cancellationToken);
}
