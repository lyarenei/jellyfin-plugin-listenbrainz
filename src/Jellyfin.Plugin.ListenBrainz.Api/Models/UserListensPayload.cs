namespace Jellyfin.Plugin.ListenBrainz.Api.Models;

/// <summary>
/// User listens response payload.
/// </summary>
public class UserListensPayload
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserListensPayload"/> class.
    /// </summary>
    public UserListensPayload()
    {
        UserId = string.Empty;
        Listens = new List<Listen>();
    }

    /// <summary>
    /// Gets or sets count of listens in this payload.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets listen's user MBID.
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// Gets or sets user's listens.
    /// </summary>
    public IEnumerable<Listen> Listens { get; set; }
}
