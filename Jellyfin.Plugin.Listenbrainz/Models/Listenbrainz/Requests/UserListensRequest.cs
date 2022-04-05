using System.Collections.Generic;
using System.Globalization;
using static Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;

/// <summary>
/// Request model for User's listens API request.
/// </summary>
public class UserListensRequest : BaseRequest
{
    private readonly string _userName;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserListensRequest"/> class.
    /// </summary>
    /// <param name="userName">User's name.</param>
    /// <param name="count">Listen count.</param>
    public UserListensRequest(string userName, int count = 5)
    {
        _userName = userName;
        Count = count;
    }

    /// <summary>
    /// Gets how many listens to request.
    /// </summary>
    private int Count { get; }

    /// <inheritdoc />
    public override Dictionary<string, string> ToRequestForm() => new() { { "count", Count.ToString(NumberFormatInfo.InvariantInfo) } };

    /// <inheritdoc />
    public override string GetEndpoint() => string.Format(CultureInfo.InvariantCulture, UserEndpoints.ListensEndpoint, _userName);
}
