namespace Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

/// <summary>
/// HttpClient used by the ListenBrainz base API client.
/// </summary>
public interface IHttpClient
{
    /// <summary>
    /// Send a HTTP request.
    /// </summary>
    /// <param name="request">Request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task with <see cref="HttpResponseMessage"/> result.</returns>
    public Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, CancellationToken cancellationToken);
}
