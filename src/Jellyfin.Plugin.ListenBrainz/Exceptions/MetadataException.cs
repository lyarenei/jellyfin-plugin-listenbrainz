namespace Jellyfin.Plugin.ListenBrainz.Exceptions;

/// <summary>
/// Exception thrown when metadata are invalid or missing.
/// </summary>
public class MetadataException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataException"/> class.
    /// </summary>
    public MetadataException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    public MetadataException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataException"/> class.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="inner">Inner exception.</param>
    public MetadataException(string message, Exception inner) : base(message, inner)
    {
    }
}
