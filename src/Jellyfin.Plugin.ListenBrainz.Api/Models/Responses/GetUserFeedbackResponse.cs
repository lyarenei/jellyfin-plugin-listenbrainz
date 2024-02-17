using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

/// <summary>
/// User feedback response.
/// </summary>
public class GetUserFeedbackResponse : IListenBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserFeedbackResponse"/> class.
    /// </summary>
    public GetUserFeedbackResponse()
    {
        Feedback = new List<Feedback>();
    }

    /// <inheritdoc />
    public bool IsOk { get; set; }

    /// <inheritdoc />
    public bool IsNotOk => !IsOk;

    /// <summary>
    /// Gets or sets count of feedbacks in this payload.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets results count offset.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Gets or sets the feedback total count.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets user's listens.
    /// </summary>
    public IEnumerable<Feedback> Feedback { get; set; }
}
