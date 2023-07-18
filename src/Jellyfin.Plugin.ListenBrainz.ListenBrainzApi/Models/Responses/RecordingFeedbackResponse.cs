using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Models.Responses;

/// <summary>
/// Recording feedback response.
/// </summary>
public class RecordingFeedbackResponse : IListenBrainzResponse
{
    /// <inheritdoc />
    public bool IsOk { get; set; }
}
