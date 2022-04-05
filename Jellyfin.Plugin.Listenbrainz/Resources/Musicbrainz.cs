namespace Jellyfin.Plugin.Listenbrainz.Resources;

/// <summary>
/// Musicbrainz API resources.
/// </summary>
public static class Musicbrainz
{
    /// <summary>
    /// API base URL.
    /// </summary>
    public const string BaseUrl = "musicbrainz.org";

    /// <summary>
    /// Basic API endpoints.
    /// </summary>
    public static class Endpoints
    {
        /// <summary>
        /// Recording endpoint.
        /// </summary>
        public const string Recording = "recording";
    }
}
