namespace Jellyfin.Plugin.ListenBrainz.Common.Exceptions;

/// <summary>
/// Exception thrown when an unrecoverable error has occurred.
/// </summary>
public class FatalException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FatalException"/> class.
    /// </summary>
    public FatalException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FatalException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    public FatalException(string msg) : base(msg)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FatalException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public FatalException(string msg, Exception inner) : base(msg, inner)
    {
    }
}
