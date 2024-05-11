using System.Globalization;
using System.Text;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;

/// <summary>
/// User listens request.
/// </summary>
public class GetUserListensRequest : IListenBrainzRequest
{
    private readonly string _userName;
    private readonly CompositeFormat _endpointFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserListensRequest"/> class.
    /// </summary>
    /// <param name="userName">Name of the user's listens.</param>
    /// <param name="listensNumber">Number of listens to fetch.</param>
    public GetUserListensRequest(string userName, int listensNumber = 10)
    {
        _endpointFormat = CompositeFormat.Parse(Endpoints.ListensEndpoint);
        _userName = userName;
        BaseUrl = General.BaseUrl;
        QueryDict = new Dictionary<string, string> { { "count", listensNumber.ToString(NumberFormatInfo.InvariantInfo) } };
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => string.Format(CultureInfo.InvariantCulture, _endpointFormat, _userName);

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public Dictionary<string, string> QueryDict { get; }
}
