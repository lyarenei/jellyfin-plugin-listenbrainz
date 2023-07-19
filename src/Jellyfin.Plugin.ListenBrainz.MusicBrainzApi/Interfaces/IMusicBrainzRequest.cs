namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;

/// <summary>
/// MusicBrainz request.
/// </summary>
public interface IMusicBrainzRequest
{
    /// <summary>
    /// Gets request endpoint.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets API base URL for this request.
    /// </summary>
    public string BaseUrl { get; init; }

    /// <summary>
    /// Gets search query data.
    /// </summary>
    public virtual Dictionary<string, string> SearchQuery => new();
}
