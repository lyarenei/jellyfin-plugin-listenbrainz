namespace Jellyfin.Plugin.ListenBrainz.Api.Resources;

/// <summary>
/// ListenBrainz API custom headers.
/// </summary>
public static class Headers
{
    /// <summary>
    /// Number of requests allowed in given time window.
    /// </summary>
    public const string RateLimitLimit = "X-RateLimit-Limit";

    /// <summary>
    /// Number of requests remaining in current time window.
    /// </summary>
    public const string RateLimitRemaining = "X-RateLimit-Remaining";

    /// <summary>
    /// Number of seconds when current time window expires (recommended: this header is resilient against clients with incorrect clocks).
    /// </summary>
    public const string RateLimitResetIn = "X-RateLimit-Reset-In";

    /// <summary>
    /// UNIX epoch number of seconds (without timezone) when current time window expires.
    /// </summary>
    public const string RateLimitReset = "X-RateLimit-Reset";
}
