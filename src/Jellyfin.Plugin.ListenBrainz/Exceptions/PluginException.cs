namespace Jellyfin.Plugin.ListenBrainz.Exceptions;

/// <summary>
/// ListenBrainz plugin general exception.
/// </summary>
public class PluginException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginException"/> class.
    /// </summary>
    public PluginException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public PluginException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public PluginException(string message, Exception inner) : base(message, inner)
    {
    }
}
