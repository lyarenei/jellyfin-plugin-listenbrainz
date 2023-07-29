using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Resources;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Models.Requests;

/// <summary>
/// Recording feedback request.
/// </summary>
public class RecordingFeedbackRequest : IListenBrainzRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingFeedbackRequest"/> class.
    /// </summary>
    public RecordingFeedbackRequest()
    {
        BaseUrl = Api.BaseUrl;
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => Endpoints.RecordingFeedback;

    /// <inheritdoc />
    public string BaseUrl { get; init; }

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