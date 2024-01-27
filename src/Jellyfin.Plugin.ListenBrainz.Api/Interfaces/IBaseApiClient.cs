namespace Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

/// <summary>
/// Base ListenBrainz API client.
/// </summary>
public interface IBaseApiClient
{
    /// <summary>
    /// Send a POST request to the ListenBrainz server.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TRequest">Data type of the request.</typeparam>
    /// <typeparam name="TResponse">Data type of the response.</typeparam>
    /// <returns>Request response.</returns>
    public Task<TResponse> SendPostRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IListenBrainzRequest
        where TResponse : IListenBrainzResponse;

    /// <summary>
    /// Send a GET request to the ListenBrainz server.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TRequest">Data type of the request.</typeparam>
    /// <typeparam name="TResponse">Data type of the response.</typeparam>
    /// <returns>Request response.</returns>
    public Task<TResponse> SendGetRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IListenBrainzRequest
        where TResponse : IListenBrainzResponse;
}
