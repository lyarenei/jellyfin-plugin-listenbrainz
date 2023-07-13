namespace Jellyfin.Plugin.ListenBrainz.Http.Exceptions;

/// <summary>
/// Exception thrown when maximum number of retries has been reached.
/// </summary>
public class RetryException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RetryException"/> class.
    /// </summary>
    public RetryException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public RetryException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public RetryException(string message, Exception inner) : base(message, inner)
    {
    }
}
