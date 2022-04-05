namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;

/// <summary>
/// Base response model for Listenbrainz responses.
/// </summary>
public class BaseResponse
{
    /// <summary>
    /// Gets or sets message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets response error.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Gets or sets response status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets HTTP response status code.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Indicates if a response is an error response.
    /// </summary>
    /// <returns>Response is error.</returns>
    public virtual bool IsError() => Code > 0 || Status != "ok";
}
