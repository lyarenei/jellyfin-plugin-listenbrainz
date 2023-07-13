using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.ListenBrainz.Http;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Interfaces;
using Jellyfin.Plugin.Listenbrainz.ListenBrainz.Json;
using Jellyfin.Plugin.Listenbrainz.ListenBrainz.Resources;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz;

/// <summary>
/// Base ListenBrainz API client.
/// </summary>
public class BaseClient : Http.Client
{
    /// <summary>
    /// Serializer options.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
    };

    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClient"/> class.
    /// </summary>
    /// <param name="baseUrl">API base URL.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    protected BaseClient(string baseUrl, IHttpClientFactory httpClientFactory, ILogger logger, ISleepService sleepService)
        : base(httpClientFactory, logger, sleepService)
    {
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Send a POST request to the ListenBrainz server.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TRequest">Data type of the request.</typeparam>
    /// <typeparam name="TResponse">Data type of the response.</typeparam>
    /// <returns>Request response.</returns>
    protected async Task<TResponse?> Post<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IListenBrainzRequest
        where TResponse : IListenBrainzResponse
    {
        var jsonData = JsonSerializer.Serialize(request, SerializerOptions);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = BuildRequestUri(request.Endpoint),
            Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("token", request.ApiToken);
        using (requestMessage) return await DoRequest<TResponse>(requestMessage, cancellationToken);
    }

    /// <summary>
    /// Send a GET request to the ListenBrainz server.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TRequest">Data type of the request.</typeparam>
    /// <typeparam name="TResponse">Data type of the response.</typeparam>
    /// <returns>Request response.</returns>
    protected async Task<TResponse?> Get<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IListenBrainzRequest
        where TResponse : IListenBrainzResponse
    {
        var requestUri = BuildRequestUri(request.Endpoint);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{requestUri}?{Utils.ToHttpGetQuery(request.QueryDict)}")
        };

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("token", request.ApiToken);
        using (requestMessage) return await DoRequest<TResponse>(requestMessage, cancellationToken);
    }

    private Uri BuildRequestUri(string endpoint) => new($"{_baseUrl}/{Api.Version}/{endpoint}");

    private async Task<TResponse?> DoRequest<TResponse>(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        where TResponse : IListenBrainzResponse
    {
        var response = await SendRequest(requestMessage, cancellationToken);
        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, SerializerOptions, cancellationToken);
        if (result is null) throw new InvalidResponseException("Response deserialized to NULL");

        result.IsOk = response.IsSuccessStatusCode;
        return result;
    }
}
