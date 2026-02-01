namespace Jellyfin.Plugin.ListenBrainz.Exceptions;

/// <summary>
/// Metadata validation exception.
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class.
    /// </summary>
    /// <param name="reason">Validation failure reason.</param>
    public ValidationException(string reason) : base(reason)
    {
    }
}
