using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;

/// <summary>
/// Response model for user listens.
/// </summary>
public class UserListensResponse : BaseResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserListensResponse"/> class.
    /// </summary>
    public UserListensResponse()
    {
        Payload = new UserListensPayload();
    }

    /// <summary>
    /// Gets or sets response payload.
    /// </summary>
    public UserListensPayload Payload { get; set; }

    /// <inheritdoc />
    public override bool IsError() => Error != null;
}

/// <summary>
/// Payload of user listens response.
/// </summary>
[SuppressMessage("Usage", "CA2227", MessageId = "Collection properties should be read only", Justification = "Used in deserialization.")]
public class UserListensPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserListensPayload"/> class.
    /// </summary>
    public UserListensPayload()
    {
        Listens = new Collection<Listen>();
    }

    /// <summary>
    /// Gets or sets listen count in response.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets UNIX timestamp of last listen in response.
    /// </summary>
    public int LatestListenTs { get; set; }

    /// <summary>
    /// Gets or sets a collection of listens in response.
    /// </summary>
    public Collection<Listen> Listens { get; set; }
}
