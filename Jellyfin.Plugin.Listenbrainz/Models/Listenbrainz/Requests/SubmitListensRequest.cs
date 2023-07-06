using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;

/// <summary>
/// Request model for submitting multiple listens.
/// </summary>
public class SubmitListensRequest : BaseRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SubmitListensRequest"/> class.
    /// </summary>
    /// <param name="listens">Listens to send.</param>
    public SubmitListensRequest(IEnumerable<Listen> listens)
    {
        ListenType = "import";
        Data = listens.ToList();
    }

    /// <summary>
    /// Gets listen type.
    /// </summary>
    public string ListenType { get; }

    /// <summary>
    /// Gets a payload for the request.
    /// </summary>
    [JsonPropertyName("payload")]
    public List<Listen> Data { get; }

    /// <inheritdoc />
    public override string GetEndpoint() => Endpoints.SubmitListen;
}
