using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;

/// <summary>
/// Validate token request.
/// </summary>
public class ValidateTokenRequest : IListenBrainzRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateTokenRequest"/> class.
    /// </summary>
    /// <param name="apiToken">API token to validate.</param>
    public ValidateTokenRequest(string apiToken)
    {
        ApiToken = apiToken;
        BaseUrl = Resources.General.BaseUrl;
        QueryDict = new Dictionary<string, string>();
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => Endpoints.ValidateToken;

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public Dictionary<string, string> QueryDict { get; }
}
