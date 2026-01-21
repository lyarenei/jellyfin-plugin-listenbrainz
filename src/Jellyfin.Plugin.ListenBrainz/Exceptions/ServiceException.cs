namespace Jellyfin.Plugin.ListenBrainz.Exceptions;

/// <summary>
/// ListenBrainz plugin service exception.
/// </summary>
public class ServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceException"/> class.
    /// </summary>
    public ServiceException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public ServiceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public ServiceException(string message, Exception inner) : base(message, inner)
    {
    }
}
