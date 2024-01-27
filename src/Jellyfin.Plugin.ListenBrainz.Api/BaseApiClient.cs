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
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed and native resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gateway.Dispose();
        }
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
        HttpResponseMessage? response;

        // TODO: update client to pass this around => have it unified
        var correlationId = Guid.NewGuid().ToString("N")[..7];

        _logger.LogDebug("({Id}) Waiting for previous request to complete (if any)", correlationId);
        await _gateway.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("({Id}) Sending request...", correlationId);
            response = await DoRequestWithRetry(requestMessage, correlationId, cancellationToken);
        }
        finally
        {
            _logger.LogDebug("({Id}) Request has been processed, freeing up resources", correlationId);
            _gateway.Release();
        }

        if (response is null)
        {
            throw new InvalidResponseException("No response is available");
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

    private async Task<HttpResponseMessage?> DoRequestWithRetry(HttpRequestMessage requestMessage, string correlationId, CancellationToken cancellationToken)
    {
        HttpResponseMessage? response = null;
        for (int i = 0; i < RateLimitAttempts; i++)
        {
            response = await _client.SendRequest(requestMessage, cancellationToken);
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                if (i + 1 == RateLimitAttempts)
                {
                    throw new ListenBrainzException(
                        $"Could not fit into a rate limit window {RateLimitAttempts} times, ({correlationId})");
                }

                _logger.LogDebug("({Id}) Rate limit reached, will retry after new window opens", correlationId);
                HandleRateLimit(response);
                continue;
            }

            _logger.LogDebug("({Id}) Did not hit any rate limits, all OK", correlationId);
            break;
        }

        return response;
    }

    private void HandleRateLimit(HttpResponseMessage response)
    {
        var header = response.Headers.FirstOrDefault(h => h.Key == Headers.RateLimitResetIn);
        var resetIn = header.Value.FirstOrDefault();
        if (resetIn is null)
        {
            throw new ListenBrainzException("No 'rate limit reset in' value available, cannot continue");
        }

        if (!int.TryParse(resetIn, out var resetInSec))
        {
            throw new ListenBrainzException("Invalid value for 'rate limit reset in', cannot continue");
        }

        _logger.LogDebug("Waiting for {Seconds} seconds before trying again", resetInSec);
        _sleepService.Sleep(resetInSec);
    }
}
