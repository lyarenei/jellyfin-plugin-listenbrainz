namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Interfaces;

/// <summary>
/// ListenBrainz request.
/// </summary>
public interface IListenBrainzRequest
{
    /// <summary>
    /// Gets API token for request authorization.
    /// </summary>
    public string? ApiToken { get; init; }

    /// <summary>
    /// Gets API endpoint.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Gets request data as a dictionary.
    /// </summary>
    public virtual Dictionary<string, string> QueryDict => new();
}
