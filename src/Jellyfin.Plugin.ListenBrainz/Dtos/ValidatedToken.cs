namespace Jellyfin.Plugin.ListenBrainz.Dtos;

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

    /// <summary>
    /// Gets a username associated with the token.
    /// </summary>
    public string? UserName { get; init; }
}
