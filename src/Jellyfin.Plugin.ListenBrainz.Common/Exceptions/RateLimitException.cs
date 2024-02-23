namespace Jellyfin.Plugin.ListenBrainz.Common.Exceptions;

/// <summary>
/// Exception thrown when a service is rate limited.
/// </summary>
public class RateLimitException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class.
    /// </summary>
    public RateLimitException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    public RateLimitException(string msg) : base(msg)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public RateLimitException(string msg, Exception inner) : base(msg, inner)
    {
    }
}
