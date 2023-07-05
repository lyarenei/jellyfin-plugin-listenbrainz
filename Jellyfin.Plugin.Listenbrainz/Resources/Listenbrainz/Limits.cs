namespace Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

/// <summary>
/// ListenBrainz limits, thresholds, etc...
/// </summary>
public static class Limits
{
    /// <summary>
    /// Maximum listens to send in a request.
    /// API docs states this limit is set to 1000, we will be a bit conservative.
    /// </summary>
    public const int MaxListensPerRequest = 100;
}
