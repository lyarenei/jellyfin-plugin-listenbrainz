namespace Jellyfin.Plugin.Listenbrainz.Resources;

/// <summary>
/// Listenbrainz API resources.
/// </summary>
public static class Listenbrainz
{
    /// <summary>
    /// API version.
    /// </summary>
    public const string ApiVersion = "1";

    /// <summary>
    /// API base URL.
    /// </summary>
    public const string BaseUrl = "api.listenbrainz.org";

    /// <summary>
    /// Basic API endpoints.
    /// </summary>
    public static class Endpoints
    {
        /// <summary>
        /// Endpoint for submitting listens.
        /// </summary>
        public const string SubmitListen = "submit-listens";

        /// <summary>
        /// Endpoint for token validation.
        /// </summary>
        public const string ValidateToken = "validate-token";
    }

    /// <summary>
    /// Feedback API endpoints.
    /// </summary>
    public static class FeedbackEndpoints
    {
        private const string EndpointBase = "feedback";

        /// <summary>
        /// Endpoint for user feedback.
        /// </summary>
        public const string RecordingFeedback = EndpointBase + "/recording-feedback";
    }

    /// <summary>
    /// User API endpoints.
    /// </summary>
    public static class UserEndpoints
    {
        private const string EndpointBase = "user";

        /// <summary>
        /// Endpoint for user listens.
        /// </summary>
        public const string ListensEndpoint = EndpointBase + "/{0}/listens";
    }
}
