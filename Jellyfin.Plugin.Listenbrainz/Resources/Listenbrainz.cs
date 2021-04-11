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
    }
}
