using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

/// <summary>
/// User listens response.
/// </summary>
public class GetUserListensResponse : IListenBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserListensResponse"/> class.
    /// </summary>
    public GetUserListensResponse()
    {
        Payload = new UserListensPayload();
    }

    /// <inheritdoc />
    public bool IsOk { get; set; }

    /// <summary>
    /// Gets or sets response payload.
    /// </summary>
    public UserListensPayload Payload { get; set; }
}
