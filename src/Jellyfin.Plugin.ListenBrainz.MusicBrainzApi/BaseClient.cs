using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Jellyfin.Plugin.ListenBrainz.Common.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Http.Services;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Json;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Resources;
using Microsoft.Extensions.Logging;
using HttpClient = Jellyfin.Plugin.ListenBrainz.Http.HttpClient;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;

/// <summary>
/// Base MusicBrainz API client.
/// </summary>
public class BaseClient : HttpClient, IDisposable
{
    /// <summary>
    /// Serializer options.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = KebabCaseNamingPolicy.Instance
    };

    private const int RateLimitAttempts = 50;
    private readonly string _clientName;
    private readonly string _clientVersion;
    private readonly string _contactUrl;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly ISleepService _sleepService;

    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClient"/> class.
    /// </summary>
    /// <param name="clientName">Name of the client.</param>
    /// <param name="clientVersion">Client version.</param>
    /// <param name="contactUrl">Link to the client.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    protected BaseClient(
        string clientName,
        string clientVersion,
        string contactUrl,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService? sleepService) : base(httpClientFactory, logger, sleepService)
    {
        _clientName = clientName;
        _clientVersion = clientVersion;
        _contactUrl = contactUrl;
        _logger = logger;
        _rateLimiter = new SemaphoreSlim(1, 1);
        _sleepService = sleepService ?? new DefaultSleepService();

        _isDisposed = false;
    }

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
            _rateLimiter.Dispose();
        }

        _isDisposed = true;
    }

    /// <summary>
    /// Send a GET request to the MusicBrainz server.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TRequest">Data type of the request.</typeparam>
    /// <typeparam name="TResponse">Data type of the response.</typeparam>
    /// <returns>Request response. Null if error.</returns>
    protected async Task<TResponse> Get<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IMusicBrainzRequest
        where TResponse : IMusicBrainzResponse
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var param in request.SearchQuery)
        {
            query[param.Key] = param.Value;
        }

        var luceneSearchQuery = request.LuceneSearchQueryString;
        if (!string.IsNullOrEmpty(luceneSearchQuery))
        {
            query["query"] = luceneSearchQuery;
        }

        var requestUri = BuildRequestUri(request.BaseUrl, request.Endpoint);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{requestUri}?{query}"),
        };

        var productValue = new ProductInfoHeaderValue(_clientName, _clientVersion);
        var commentValue = new ProductInfoHeaderValue($"( {_contactUrl} )");
        var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");

        requestMessage.Headers.UserAgent.Add(productValue);
        requestMessage.Headers.UserAgent.Add(commentValue);
        requestMessage.Headers.Accept.Add(acceptHeader);
        using (requestMessage) return await DoRequest<TResponse>(requestMessage, cancellationToken);
    }

    private static Uri BuildRequestUri(string baseUrl, string endpoint) =>
        new($"{baseUrl}/ws/{Api.Version}/{endpoint}");

    private async Task<TResponse> DoRequest<TResponse>(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
        where TResponse : IMusicBrainzResponse
    {
        using var scope = _logger.AddNewScope("ClientRequestId");
        HttpResponseMessage response;
        _logger.LogDebug("Waiting for previous request to complete (if any)");
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            _logger.LogDebug("Sending request...");
            response = await DoRequestWithRetry(requestMessage, cancellationToken);
        }
        finally
        {
            _logger.LogDebug("Request has been processed, freeing up resources");
            _rateLimiter.Release();
        }

        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer
            .DeserializeAsync<TResponse>(responseStream, SerializerOptions, cancellationToken);
        if (result is null)
        {
            throw new NoDataException("Response deserialized to NULL");
        }

        return result;
    }

    /// <summary>
    /// Convert dictionary to Musicbrainz query.
    /// </summary>
    /// <param name="requestData">Query data.</param>
    /// <returns>Query string.</returns>
    private static string ToMusicbrainzQuery(Dictionary<string, string> requestData)
    {
        var query = string.Empty;
        var i = 0;
        foreach (var d in requestData)
        {
            query += HttpUtility.UrlEncode($"{d.Key}:{d.Value}");
            if (++i != requestData.Count)
            {
                query += HttpUtility.UrlEncode(" AND ");
            }
        }

        return query;
    }

    private async Task<HttpResponseMessage> DoRequestWithRetry(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < RateLimitAttempts; i++)
        {
            var response = await SendRequest(requestMessage, cancellationToken);

            // MusicBrainz will return 503 if over rate limit
            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                if (i + 1 == RateLimitAttempts)
                {
                    throw new RateLimitException($"Could not fit into a rate limit window {RateLimitAttempts} times");
                }

                _logger.LogDebug("Rate limit reached, will retry after new window opens");
                await HandleRateLimit();
                continue;
            }

            _logger.LogDebug("Did not hit any rate limits, all OK");
            return response;
        }

        throw new InvalidResponseException("No response available from MusicBrainz server");
    }

    private async Task HandleRateLimit()
    {
        // MusicBrainz documentation says the rate limit is on average 1 rps.
        await _sleepService.SleepAsync(1);
    }
}
