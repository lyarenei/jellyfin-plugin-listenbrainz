using Newtonsoft.Json;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Interfaces;

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
    /// Gets request data as a dictionary.
    /// </summary>
    [JsonIgnore]
    public virtual Dictionary<string, string> QueryDict => new();
}
