namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Exceptions;

/// <summary>
/// Exception for various invalid ListenBrainz stuff.
/// </summary>
public class ListenBrainzException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzException"/> class.
    /// </summary>
    public ListenBrainzException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public ListenBrainzException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public ListenBrainzException(string message, Exception inner) : base(message, inner)
    {
    }
}
