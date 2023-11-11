using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Api;

/// <inheritdoc />
public class HttpClientWrapper : IHttpClient
{
    private readonly Http.HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientWrapper"/> class.
    /// </summary>
    /// <param name="client">Underlying HTTP client.</param>
    public HttpClientWrapper(Http.HttpClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _client.SendRequest(request, cancellationToken);
    }
}
