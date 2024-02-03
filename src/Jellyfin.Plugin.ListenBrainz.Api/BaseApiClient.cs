using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Jellyfin.Plugin.ListenBrainz.Api.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Http;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Http.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.Api;

/// <summary>
/// Base ListenBrainz API client.
/// </summary>
public class BaseApiClient : IBaseApiClient, IDisposable
{
    private const int RateLimitAttempts = 50;
    private readonly IHttpClient _client;
    private readonly SemaphoreSlim _gateway;
    private readonly ILogger _logger;
    private readonly ISleepService _sleepService;
    private bool _isDisposed;

    /// <summary>
    /// Serializer settings.
    /// </summary>
    public static readonly JsonSerializerSettings SerializerSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include,
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseApiClient"/> class.
    /// </summary>
    /// <param name="client">Underlying HTTP client.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public BaseApiClient(IHttpClient client, ILogger logger, ISleepService? sleepService)
    {
        _client = client;
        _logger = logger;
        _sleepService = sleepService ?? new DefaultSleepService();
        _gateway = new SemaphoreSlim(1, 1);
        _isDisposed = false;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="BaseApiClient"/> class.
    /// </summary>
    ~BaseApiClient() => Dispose(false);

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and unmanaged (own) resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _gateway.Dispose();
        }

        _isDisposed = true;
    }

    /// <inheritdoc />
    public async Task<TResponse> SendPostRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
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
    public async Task<TResponse> SendGetRequest<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
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

    private static Uri BuildRequestUri(string baseUrl, string endpoint) => new($"{baseUrl}/{General.Version}/{endpoint}");

    private async Task<TResponse> DoRequest<TResponse>(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        where TResponse : IListenBrainzResponse
    {
        var requestId = Guid.NewGuid().ToString("N")[..7];
        using var scope = _logger.BeginScope(new Dictionary<string, string> { { "clientRequestId", requestId } });
        HttpResponseMessage response;

        _logger.LogDebug("Waiting for previous request to complete (if any)");
        await _gateway.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Sending request...");
            response = await DoRequestWithRetry(requestMessage, cancellationToken);
        }
        finally
        {
            _logger.LogDebug("Request has been processed, freeing up resources");
            _gateway.Release();
        }

        var stringContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonConvert.DeserializeObject<TResponse>(stringContent, SerializerSettings);
        if (result is null)
        {
            throw new InvalidResponseException("Failed to parse JSON data from response");
        }

        result.IsOk = response.IsSuccessStatusCode;
        return result;
    }

    private async Task<HttpResponseMessage> DoRequestWithRetry(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
    {
        for (int i = 0; i < RateLimitAttempts; i++)
        {
            var response = await _client.SendRequest(requestMessage, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (i + 1 == RateLimitAttempts)
                {
                    throw new ListenBrainzException($"Could not fit into a rate limit window {RateLimitAttempts} times");
                }

                _logger.LogDebug("Rate limit reached, will retry after new window opens");
                await HandleRateLimit(response);
                continue;
            }

            _logger.LogDebug("Did not hit any rate limits, all OK");
            return response;
        }

        throw new ListenBrainzException("No response available from the server");
    }

    private async Task HandleRateLimit(HttpResponseMessage response)
    {
        var header = response.Headers.FirstOrDefault(h => h.Key == Headers.RateLimitResetIn);
        var resetIn = header.Value.FirstOrDefault();
        if (resetIn is null)
        {
            throw new ListenBrainzException("No 'rate limit reset in' header value available");
        }

        if (!int.TryParse(resetIn, out var resetInSec))
        {
            throw new ListenBrainzException("Invalid value for 'rate limit reset in' header");
        }

        _logger.LogDebug("Waiting for {Seconds} seconds before trying again", resetInSec);
        await _sleepService.SleepAsync(resetInSec);
    }
}
