using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;

/// <summary>
/// Response model for validating token.
/// </summary>
public class ValidateTokenResponse : BaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateTokenResponse"/> class.
    /// </summary>
    public ValidateTokenResponse()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Gets or sets user name associated with the token.
    /// </summary>
    [JsonPropertyName("user_name")]
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether token is valid.
    /// </summary>
    public bool Valid { get; set; }

    /// <inheritdoc />
    public override bool IsError() => !Valid || Code == 400;
}
