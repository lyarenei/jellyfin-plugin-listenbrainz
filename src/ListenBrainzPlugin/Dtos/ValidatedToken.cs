namespace ListenBrainzPlugin.Dtos;

/// <summary>
/// Validated token.
/// </summary>
public class ValidatedToken
{
    /// <summary>
    /// Gets a value indicating whether token is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets the reason of token being invalid.
    /// </summary>
    public string? Reason { get; init; }
}
