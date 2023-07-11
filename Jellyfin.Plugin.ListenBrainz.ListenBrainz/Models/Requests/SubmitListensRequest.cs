using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Dtos;
using Jellyfin.Plugin.Listenbrainz.ListenBrainz.Resources;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Requests;

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
        Payload = new List<Listen>();
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => Endpoints.SubmitListens;

    /// <summary>
    /// Gets or sets listen type.
    /// </summary>
    public ListenType ListenType { get; set; }

    /// <summary>
    /// Gets or sets request payload.
    /// </summary>
    public IEnumerable<Listen> Payload { get; set; }
}
