namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses;

/// <summary>
/// Base response model for Musicbrainz responses.
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Gets or sets error message.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Indicates if a response is an error response.
    /// </summary>
    /// <returns>Response is error.</returns>
    public virtual bool IsError() => !string.IsNullOrEmpty(Error);
}
