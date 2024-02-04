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
        Payload = new UserFeedbackPayload();
    }

    /// <inheritdoc />
    public bool IsOk { get; set; }

    /// <inheritdoc />
    public bool IsNotOk => !IsOk;

    /// <summary>
    /// Gets or sets response payload.
    /// </summary>
    public UserFeedbackPayload Payload { get; set; }
}
