using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

/// <summary>
/// User listens response.
/// </summary>
public class UserListensResponse : IListenBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserListensResponse"/> class.
    /// </summary>
    public UserListensResponse()
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
