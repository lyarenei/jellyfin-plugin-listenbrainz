using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients
{
    /// <summary>
    /// Base HTTP client for sending requests.
    /// </summary>
    public class BaseHttpClient
    {
        private const int RetryBackoffSeconds = 3;
        private const int RetryCount = 6;

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
        /// Initializes a new instance of the <see cref="BaseHttpClient"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory for creating the clients.</param>
        /// <param name="logger">Logger instance for logging.</param>
        /// <param name="sleepService">Sleep service.</param>
        public BaseHttpClient(IHttpClientFactory httpClientFactory, ILogger logger, ISleepService sleepService)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _sleepService = sleepService;
        }

        /// <summary>
        /// Send a HTTP request.
        /// </summary>
        /// <param name="request">The request to send.</param>
        /// <returns>A request response.</returns>
        /// <exception cref="InvalidResponseException">Invalid response.</exception>
        /// <exception cref="RetryException">Retry limit reached.</exception>
        protected async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request)
        {
            using var httpClient = _httpClientFactory.CreateClient();
            HttpResponseMessage? response = null;
            var retrySecs = 1;
            var requestId = Guid.NewGuid().ToString("N")[..7];
            for (var retryCount = 0; retryCount < RetryCount; retryCount++)
            {
                using var requestMessage = await Clone(request).ConfigureAwait(false);
                LogRequest(requestMessage, requestId);

                response = await httpClient.SendAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);
                if (!_retryStatuses.Contains(response.StatusCode))
                {
                    _logger.LogDebug(
                        "Response status is {Status}, will not retry ({RequestID})",
                        response.StatusCode,
                        requestId);
                    break;
                }

                if (retryCount + 1 == RetryCount)
                {
                    _logger.LogInformation("Retry limit reached, giving up ({RequestID})", requestId);
                    throw new RetryException("Retry limit reached");
                }

                retrySecs *= RetryBackoffSeconds;
                _logger.LogWarning("Request failed, will retry after {Num} seconds ({RequestID})", retrySecs, requestId);
                _sleepService.Sleep(retrySecs);
            }

            if (response == null)
            {
                _logger.LogError("Should not reach here - response is null ({RequestId})", requestId);
                throw new InvalidResponseException("response is null");
            }

            LogResponse(response, requestId);
            return response;
        }

        /// <summary>
        /// Clones a HttpRequestMessage.
        /// Inspired by https://stackoverflow.com/a/65435043.
        /// </summary>
        /// <param name="httpRequestMessage">HTTP request to clone.</param>
        /// <returns>HTTP request clone.</returns>
        private static async Task<HttpRequestMessage> Clone(HttpRequestMessage httpRequestMessage)
        {
            var messageClone = new HttpRequestMessage(httpRequestMessage.Method, httpRequestMessage.RequestUri);
            if (httpRequestMessage.Content != null)
            {
                var ms = new MemoryStream();
                await httpRequestMessage.Content.CopyToAsync(ms).ConfigureAwait(false);
                ms.Position = 0;
                messageClone.Content = new StreamContent(ms);
                httpRequestMessage.Content.Headers.ToList()
                    .ForEach(header => messageClone.Content.Headers.Add(header.Key, header.Value));
            }

            messageClone.Version = httpRequestMessage.Version;

            httpRequestMessage.Options.ToList().ForEach(option =>
                messageClone.Options.Set(new HttpRequestOptionsKey<string?>(option.Key), option.Value?.ToString()));
            httpRequestMessage.Headers.ToList().ForEach(header =>
                messageClone.Headers.TryAddWithoutValidation(header.Key, header.Value));

            return messageClone;
        }

        private void LogRequest(HttpRequestMessage requestMessage, string id)
        {
            var requestData = requestMessage.Content?.ReadAsStringAsync();
            _logger.LogDebug(
                "Sending request ({RequestId}):\n" +
                "Method: {Method}\n" +
                "URI: {Uri}\n" +
                "Data: {Data}",
                id,
                requestMessage.Method,
                requestMessage.RequestUri,
                requestData?.Result);
        }

        private void LogResponse(HttpResponseMessage? responseMessage, string id)
        {
            var responseData = responseMessage?.Content.ReadAsStringAsync();
            _logger.LogDebug(
                "Got response ({RequestId}):\n" +
                "Status: {Status}\n" +
                "Data: {Data}",
                id,
                responseMessage?.StatusCode,
                responseData?.Result);
        }
    }

    /// <summary>
    /// The exception that is thrown when retry limit has been reached.
    /// </summary>
    public class RetryException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryException"/> class.
        /// </summary>
        /// <param name="msg">Exception message.</param>
        public RetryException(string msg) : base(msg)
        {
        }
    }

    /// <summary>
    /// The exception that is thrown when there was an invalid response.
    /// </summary>
    public class InvalidResponseException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InvalidResponseException"/> class.
        /// </summary>
        /// <param name="msg">Exception message.</param>
        public InvalidResponseException(string msg) : base(msg)
        {
        }
    }
}
