namespace Jellyfin.Plugin.ListenBrainz.Api.Models;

/// <summary>
/// User feedback response payload.
/// </summary>
public class UserFeedbackPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserFeedbackPayload"/> class.
    /// </summary>
    public UserFeedbackPayload()
    {
        Feedback = new List<Feedback>();
    }

    /// <summary>
    /// Gets or sets count of feedbacks in this payload.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int Offset { get; set; }
    /// <summary>
    ///
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets user's listens.
    /// </summary>
    public IEnumerable<Feedback> Feedback { get; set; }
}
