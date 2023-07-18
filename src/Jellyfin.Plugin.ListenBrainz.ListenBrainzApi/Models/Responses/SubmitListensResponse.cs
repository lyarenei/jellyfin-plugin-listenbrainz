using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Models.Responses;

/// <summary>
/// Submit listens response.
/// </summary>
public class SubmitListensResponse : IListenBrainzResponse
{
    /// <inheritdoc />
    public bool IsOk { get; set; }
}
