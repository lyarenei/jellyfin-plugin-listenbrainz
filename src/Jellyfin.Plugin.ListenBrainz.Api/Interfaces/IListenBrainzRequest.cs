using Newtonsoft.Json;

namespace Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

/// <summary>
/// ListenBrainz request.
/// </summary>
public interface IListenBrainzRequest
{
    /// <summary>
    /// Gets API token for request authorization.
    /// </summary>
    [JsonIgnore]
    public string? ApiToken { get; init; }

    /// <summary>
    /// Gets API endpoint.
    /// </summary>
    [JsonIgnore]
    public string Endpoint { get; }

    /// <summary>
    /// Gets API base URL.
    /// </summary>
    [JsonIgnore]
    public string BaseUrl { get; init; }

    /// <summary>
    /// Gets request data as a dictionary.
    /// </summary>
    [JsonIgnore]
    public virtual Dictionary<string, string> QueryDict => new();
}
