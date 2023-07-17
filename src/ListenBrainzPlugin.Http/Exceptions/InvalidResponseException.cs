namespace ListenBrainzPlugin.Http.Exceptions;

/// <summary>
/// Exception thrown when maximum number of retries has been reached.
/// </summary>
public class InvalidResponseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidResponseException"/> class.
    /// </summary>
    public InvalidResponseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidResponseException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public InvalidResponseException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidResponseException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public InvalidResponseException(string message, Exception inner) : base(message, inner)
    {
    }
}
