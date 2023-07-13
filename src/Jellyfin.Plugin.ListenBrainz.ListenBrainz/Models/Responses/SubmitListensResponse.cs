using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Responses;

/// <summary>
/// Submit listens response.
/// </summary>
public class SubmitListensResponse : IListenBrainzResponse
{
    /// <inheritdoc />
    public bool IsOk { get; set; }
}
