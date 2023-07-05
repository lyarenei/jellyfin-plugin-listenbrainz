namespace Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz
{
    /// <summary>
    /// Listenbrainz API resources.
    /// </summary>
    public static class Api
    {
        /// <summary>
        /// API version.
        /// </summary>
        public const string Version = "1";

        /// <summary>
        /// API base URL.
        /// </summary>
        public const string BaseUrl = "https://api.listenbrainz.org";

        /// <summary>
        /// Maximum listens to send in a request.
        /// API docs states this limit is set to 1000, we will be a bit conservative.
        /// </summary>
        public const int MaxListensPerRequest = 100;
    }
}
