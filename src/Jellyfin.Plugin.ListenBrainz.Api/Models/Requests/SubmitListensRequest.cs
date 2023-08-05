using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;

/// <summary>
/// Submit listens request.
/// </summary>
public class SubmitListensRequest : IListenBrainzRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitListensRequest"/> class.
    /// </summary>
    public SubmitListensRequest()
    {
        ListenType = ListenType.PlayingNow;
        BaseUrl = Resources.General.BaseUrl;
        Payload = new List<Listen>();
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => Endpoints.SubmitListens;

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <summary>
    /// Gets listen type.
    /// </summary>
    [JsonIgnore]
    public ListenType ListenType { get; init; }

    /// <summary>
    /// Gets <see cref="ListenType"/> as a string.
    /// </summary>
    [JsonProperty("listen_type")]
    public string ListenTypeString => ListenType.Value;

    /// <summary>
    /// Gets or sets request payload.
    /// </summary>
    public IEnumerable<Listen> Payload { get; set; }
}
