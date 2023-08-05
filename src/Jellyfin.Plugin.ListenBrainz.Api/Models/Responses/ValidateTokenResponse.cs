using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

/// <summary>
/// Validate token response.
/// </summary>
public class ValidateTokenResponse : IListenBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateTokenResponse"/> class.
    /// </summary>
    public ValidateTokenResponse()
    {
        Code = string.Empty;
        Message = string.Empty;
        Valid = false;
    }

    /// <inheritdoc />
    public bool IsOk { get; set; }

    /// <summary>
    /// Gets or sets status code.
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Gets or sets additional message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether token is valid.
    /// </summary>
    public bool Valid { get; set; }

    /// <summary>
    /// Gets or sets user name associated with the token.
    /// </summary>
    public string? UserName { get; set; }
}
