using ListenBrainzPlugin.ListenBrainzApi.Interfaces;
using ListenBrainzPlugin.ListenBrainzApi.Resources;

namespace ListenBrainzPlugin.ListenBrainzApi.Models.Requests;

/// <summary>
/// Recording feedback request.
/// </summary>
public class RecordingFeedbackRequest : IListenBrainzRequest
{
    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => Endpoints.RecordingFeedback;

    /// <summary>
    /// Gets or sets MBID of the recording.
    /// </summary>
    public string? RecordingMbid { get; set; }

    /// <summary>
    /// Gets or sets MSID of the recording.
    /// </summary>
    public string? RecordingMsid { get; set; }

    /// <summary>
    /// Gets or sets feedback score.
    /// </summary>
    public FeedbackScore Score { get; set; }
}
