namespace Jellyfin.Plugin.Listenbrainz.Resources
{
    public static class Listenbrainz
    {
        public const string BaseUrl = "api.listenbrainz.org";

        public static class Endpoints
        {
            public const string SubmitListen = "submit-listens";
            public const string ValidateToken = "validate-token";
        }

        public static class FeedbackEndpoints
        {
            private const string EndpointBase = "feedback";
            public const string RecordingFeedback = EndpointBase + "/recording-feedback";
        }

        public static class UserEndpoints
        {
            private const string EndpointBase = "user";
            public const string ListensEndpoint = EndpointBase + "/{0}/listens";
        }
    }
}
