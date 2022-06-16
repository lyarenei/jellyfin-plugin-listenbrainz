namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests;

/// <summary>
/// Base model for Musicbrainz requests.
/// </summary>
public class BaseRequest
{
    /// <summary>
    /// Converts request data to a form suitable for use as request data.
    /// </summary>
    /// <returns>Object suitable for use as request data.</returns>
    public virtual object ToRequestForm() => this;

    /// <summary>
    /// Get endpoint name of this request.
    /// </summary>
    /// <returns>Endpoint name.</returns>
    public virtual string GetEndpoint() => string.Empty;
}
