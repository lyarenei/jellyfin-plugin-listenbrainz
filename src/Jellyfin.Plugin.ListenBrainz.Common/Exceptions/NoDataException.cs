namespace Jellyfin.Plugin.ListenBrainz.Common.Exceptions;

/// <summary>
/// Exception thrown when there's no data available.
/// </summary>
public class NoDataException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoDataException"/> class.
    /// </summary>
    public NoDataException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoDataException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    public NoDataException(string msg) : base(msg)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NoDataException"/> class.
    /// </summary>
    /// <param name="msg">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public NoDataException(string msg, Exception inner) : base(msg, inner)
    {
    }
}
