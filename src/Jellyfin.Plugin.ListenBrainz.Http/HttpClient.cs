using System.Net;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Http.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Http;

/// <summary>
/// A custom HTTP client.
/// Can be used as a base for application-specific API clients.
/// </summary>
public class HttpClient
{
    private const int RetryBackoffSeconds = 3;
    private const int MaxRetries = 6;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ISleepService _sleepService;

    private readonly List<HttpStatusCode> _retryStatuses = new()
    {
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.InsufficientStorage
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public HttpClient(IHttpClientFactory httpClientFactory, ILogger logger, ISleepService? sleepService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _sleepService = sleepService ?? new DefaultSleepService();
    }

    /// <summary>
    /// Send a HTTP request.
    /// </summary>
    /// <param name="requestMessage">Request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Request response.</returns>
    /// <exception cref="RetryException">Number of retries has been reached.</exception>
    /// <exception cref="InvalidResponseException">Response is not available.</exception>
    public async Task<HttpResponseMessage> SendRequest(
        HttpRequestMessage requestMessage,
        CancellationToken cancellationToken)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        var requestId = Guid.NewGuid().ToString("N")[..7];
        using var scope = _logger.BeginScope(new Dictionary<string, object> { { "HttpRequestId", requestId } });
        var retrySecs = 1;

        HttpResponseMessage? responseMessage = null;
        for (var retries = 0; retries < MaxRetries; retries++)
        {
            using var request = await Clone(requestMessage);
            await LogRequest(request);

            try
            {
                responseMessage = await httpClient.SendAsync(request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Request has been cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("An error occured when sending a request: {Reason}", ex.Message);
                _logger.LogDebug(ex, "An exception was thrown when sending a request");
                break;
            }

            if (responseMessage is not null && !_retryStatuses.Contains(responseMessage.StatusCode))
            {
                _logger.LogDebug("Response status is {Status}, will not retry", responseMessage.StatusCode);
                break;
            }

            if (retries + 1 == MaxRetries) throw new RetryException("Retry limit reached");

            retrySecs *= RetryBackoffSeconds;
            _logger.LogWarning("Request failed, will retry after {Num} seconds", retrySecs);
            await _sleepService.SleepAsync(retrySecs);
        }

        if (responseMessage is null)
        {
            _logger.LogWarning("No response available for the request");
            throw new InvalidResponseException("Response is null");
        }

        await LogResponse(responseMessage);
        return responseMessage;
    }

    /// <summary>
    /// Clones a <see cref="HttpRequestMessage"/>.
    /// Inspired by https://stackoverflow.com/a/65435043.
    /// </summary>
    /// <param name="originalRequest">HTTP request to clone.</param>
    /// <returns>HTTP request clone.</returns>
    private static async Task<HttpRequestMessage> Clone(HttpRequestMessage originalRequest)
    {
        var clonedRequest = new HttpRequestMessage(originalRequest.Method, originalRequest.RequestUri);
        if (originalRequest.Content is not null)
        {
            var stream = new MemoryStream();
            await originalRequest.Content.CopyToAsync(stream);
            stream.Position = 0;
            clonedRequest.Content = new StreamContent(stream);
            originalRequest.Content.Headers.ToList()
                .ForEach(header => clonedRequest.Content.Headers.Add(header.Key, header.Value));
        }

        clonedRequest.Version = originalRequest.Version;

        originalRequest.Options.ToList().ForEach(option =>
            clonedRequest.Options.Set(new HttpRequestOptionsKey<string?>(option.Key), option.Value?.ToString()));

        originalRequest.Headers.ToList().ForEach(header =>
            clonedRequest.Headers.TryAddWithoutValidation(header.Key, header.Value));

        return clonedRequest;
    }

    private async Task LogRequest(HttpRequestMessage requestMessage)
    {
        var requestData = "null";
        if (requestMessage.Content is not null) requestData = await requestMessage.Content.ReadAsStringAsync();

        _logger.LogDebug(
            "Sending request:\nMethod: {Method}\nURI: {Uri}\nData: {Data}",
            requestMessage.Method,
            requestMessage.RequestUri,
            requestData);
    }

    private async Task LogResponse(HttpResponseMessage responseMessage)
    {
        var responseData = await responseMessage.Content.ReadAsStringAsync();
        _logger.LogDebug(
            "Got response:\nStatus: {Status}\nData: {Data}",
            responseMessage.StatusCode,
            responseData);
    }
}
