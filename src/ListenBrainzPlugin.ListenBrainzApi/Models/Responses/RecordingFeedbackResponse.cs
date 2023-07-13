using ListenBrainzPlugin.ListenBrainzApi.Interfaces;

namespace ListenBrainzPlugin.ListenBrainzApi.Models.Responses;

/// <summary>
/// Recording feedback response.
/// </summary>
public class RecordingFeedbackResponse : IListenBrainzResponse
{
    /// <inheritdoc />
    public bool IsOk { get; set; }
}
