namespace Jellyfin.Plugin.ListenBrainz.Api.Resources;

/// <summary>
/// ListenBrainz API endpoints.
/// </summary>
public static class Endpoints
{
    /// <summary>
    /// Endpoint for submitting listens.
    /// </summary>
    public const string SubmitListens = "submit-listens";

    /// <summary>
    /// Endpoint for token validation.
    /// </summary>
    public const string ValidateToken = "validate-token";

    /// <summary>
    /// Feedback endpoint base.
    /// </summary>
    private const string FeedbackEndpointBase = "feedback";

    /// <summary>
    /// Endpoint for recording feedback.
    /// </summary>
    public const string RecordingFeedback = FeedbackEndpointBase + "/recording-feedback";

    /// <summary>
    /// User endpoint base.
    /// </summary>
    private const string UserEndpointBase = "user";

    /// <summary>
    /// Endpoint for user listens.
    /// </summary>
    public const string ListensEndpoint = UserEndpointBase + "/{0}/listens";
}
