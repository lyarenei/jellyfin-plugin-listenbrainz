namespace Jellyfin.Plugin.Listenbrainz.Resources.ListenBrainz
{
    /// <summary>
    /// Listenbrainz API endpoints.
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

        /// <summary>
        /// Feedback endpoint base.
        /// </summary>
        private const string FeedbackEndpointBase = "feedback";

        /// <summary>
        /// Endpoint for user feedback.
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
}
