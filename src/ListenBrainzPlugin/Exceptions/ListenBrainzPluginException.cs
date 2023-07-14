namespace ListenBrainzPlugin.Exceptions;

/// <summary>
/// ListenBrainz plugin general exception.
/// </summary>
public class ListenBrainzPluginException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzPluginException"/> class.
    /// </summary>
    public ListenBrainzPluginException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzPluginException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public ListenBrainzPluginException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzPluginException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public ListenBrainzPluginException(string message, Exception inner) : base(message, inner)
    {
    }
}
