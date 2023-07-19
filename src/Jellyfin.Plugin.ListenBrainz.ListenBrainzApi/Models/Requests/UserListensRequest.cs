using System.Globalization;
using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Resources;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainzApi.Models.Requests;

/// <summary>
/// User listens request.
/// </summary>
public class UserListensRequest : IListenBrainzRequest
{
    private readonly string _userName;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserListensRequest"/> class.
    /// </summary>
    /// <param name="userName">Name of the user's listens.</param>
    /// <param name="listensNumber">Number of listens to fetch.</param>
    public UserListensRequest(string userName, int listensNumber = 10)
    {
        _userName = userName;
        BaseUrl = Api.BaseUrl;
        QueryDict = new Dictionary<string, string> { { "count", listensNumber.ToString(NumberFormatInfo.InvariantInfo) } };
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => string.Format(CultureInfo.InvariantCulture, Endpoints.ListensEndpoint, _userName);

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public Dictionary<string, string> QueryDict { get; }
}
