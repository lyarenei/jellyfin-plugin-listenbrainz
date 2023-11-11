using System.Net.Http.Headers;
using System.Text;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Http;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.Api;

/// <summary>
/// Base ListenBrainz API client.
/// </summary>
public class BaseApiClient : IBaseApiClient
{
    /// <summary>
    /// Serializer settings.
    /// </summary>
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include,
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    private readonly IHttpClient _client;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseApiClient"/> class.
    /// </summary>
    /// <param name="client">Underlying HTTP client.</param>
    /// <param name="logger">Logger instance.</param>
    public BaseApiClient(IHttpClient client, ILogger logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse?> SendPostRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IListenBrainzRequest
        where TResponse : IListenBrainzResponse
    {
        var jsonData = JsonConvert.SerializeObject(request, SerializerSettings);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = BuildRequestUri(request.BaseUrl, request.Endpoint),
            Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("token", request.ApiToken);
        using (requestMessage) return await DoRequest<TResponse>(requestMessage, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResponse?> SendGetRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IListenBrainzRequest
        where TResponse : IListenBrainzResponse
    {
        var requestUri = BuildRequestUri(request.BaseUrl, request.Endpoint);
        var queryParams = Utils.ToHttpGetQuery(request.QueryDict);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = request.QueryDict.Any() ? new Uri($"{requestUri}?{queryParams}") : new Uri(requestUri.ToString())
        };

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("token", request.ApiToken);
        using (requestMessage) return await DoRequest<TResponse>(requestMessage, cancellationToken);
    }

    private Uri BuildRequestUri(string baseUrl, string endpoint) => new($"{baseUrl}/{Resources.General.Version}/{endpoint}");

    private async Task<TResponse?> DoRequest<TResponse>(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        where TResponse : IListenBrainzResponse
    {
        var response = await _client.SendRequest(requestMessage, cancellationToken);
        var stringContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonConvert.DeserializeObject<TResponse>(stringContent, SerializerSettings);
        if (result is null) throw new InvalidResponseException("Response deserialized to NULL");

        result.IsOk = response.IsSuccessStatusCode;
        return result;
    }
}
