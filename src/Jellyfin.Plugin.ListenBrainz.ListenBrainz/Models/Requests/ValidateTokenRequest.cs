using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Interfaces;
using Jellyfin.Plugin.Listenbrainz.ListenBrainz.Resources;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Requests;

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
        Endpoint = Endpoints.ValidateToken;
        QueryDict = new Dictionary<string, string>();
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint { get; }

    /// <inheritdoc />
    public Dictionary<string, string> QueryDict { get; }
}
