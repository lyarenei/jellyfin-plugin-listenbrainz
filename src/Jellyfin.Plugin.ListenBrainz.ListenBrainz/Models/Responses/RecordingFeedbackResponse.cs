using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Responses;

/// <summary>
/// Recording feedback response.
/// </summary>
public class RecordingFeedbackResponse : IListenBrainzResponse
{
    /// <inheritdoc />
    public bool IsOk { get; set; }
}
