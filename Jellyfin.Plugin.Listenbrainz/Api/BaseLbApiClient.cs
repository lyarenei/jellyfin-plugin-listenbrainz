using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Api
{
    public class BaseLbApiClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly List<HttpStatusCode> _retryStatuses = new()
        {
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout,
            HttpStatusCode.InsufficientStorage
        };

        private const int RetryBackoffSeconds = 3;
        private const int RetryCount = 6;

        public BaseLbApiClient(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient();
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        /// <summary>
        /// Send a POST request to the Listenbrainz server
        /// </summary>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="request">The request</param>
        /// <returns>A response with type TResponse</returns>
        public async Task<TResponse> Post<TRequest, TResponse>(TRequest request) where TRequest : BaseRequest where TResponse : BaseResponse
        {
            var jsonData = _jsonSerializer.SerializeToString(request.ToRequestForm());
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", request.ApiToken);
            HttpResponseMessage response = null;
            var retrySecs = 1;

            for (var retryCount = 0; retryCount < RetryCount; retryCount++)
            {
                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(BuildRequestUrl(request.GetEndpoint())),
                    Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
                };

                LogRequest(requestMessage);
                response = await _httpClient.SendAsync(requestMessage, CancellationToken.None);
                if (!_retryStatuses.Contains(response.StatusCode))
                {
                    _logger.LogDebug("Response status is {Status}, will not retry", response.StatusCode);
                    break;
                }

                if (retryCount + 1 == RetryCount)
                {
                    _logger.LogInformation("Retry count limit reached, giving up");
                    break;
                }

                retrySecs *= RetryBackoffSeconds;
                _logger.LogWarning("Request failed, will retry after {Num} seconds", retrySecs);
                Thread.Sleep(retrySecs * 1000);
            }

            if (response == null)
            {
                _logger.LogError("Should not reach here - response is null");
                return null;
            }

            LogResponse(response);
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                try
                {
                    var result = _jsonSerializer.DeserializeFromStream<TResponse>(stream);
                    if (result.IsError())
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        var reader = new StreamReader(stream);
                        var text = reader.ReadToEnd();
                        _logger.LogDebug($"Raw response: {text}");

                        _logger.LogDebug(result.Code.ToString());
                        _logger.LogDebug(result.Message);
                        _logger.LogDebug(result.Error);
                    }

                    return result;
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }

            return null;
        }

        public async Task<TResponse> Get<TRequest, TResponse>(TRequest request) where TRequest : BaseRequest where TResponse : BaseResponse
        {
            return await Get<TRequest, TResponse>(request, CancellationToken.None);
        }

        public async Task<TResponse> Get<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken) where TRequest : BaseRequest where TResponse : BaseResponse
        {
            var requestData = request.ToRequestForm();
            string url = $"{BuildRequestUrl(request.GetEndpoint())}?";

            int i = 0;
            foreach (var d in requestData)
            {
                url += $"{d.Key}={d.Value}";
                if (++i != requestData.Count)
                    url += '&';
            }

            HttpResponseMessage response = null;
            var retrySecs = 1;
            for (var retryCount = 0; retryCount < RetryCount; retryCount++)
            {
                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri(url)
                };

                LogRequest(requestMessage);
                response = await _httpClient.SendAsync(requestMessage, CancellationToken.None);
                if (!_retryStatuses.Contains(response.StatusCode))
                {
                    _logger.LogDebug("Response status is {Status}, will not retry", response.StatusCode);
                    break;
                }

                if (retryCount + 1 == RetryCount)
                {
                    _logger.LogInformation("Retry count limit reached, giving up");
                    break;
                }

                retrySecs *= RetryBackoffSeconds;
                _logger.LogWarning("Request failed, will retry after {Num} seconds", retrySecs);
                Thread.Sleep(retrySecs * 1000);
            }
            
            if (response == null)
            {
                _logger.LogError("Should not reach here - response is null");
                return null;
            }

            LogResponse(response);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var reader = new StreamReader(stream);
            var text = reader.ReadToEnd();

            try
            {
                var result = JsonSerializer.Deserialize<TResponse>(text);

                _logger.LogDebug($"Raw response: {text}");
                _logger.LogDebug($"Response code (not HTTP status code): {result.Code}");
                _logger.LogDebug($"Response message: {result.Message}");
                _logger.LogDebug($"Response error: {result.Error}");

                return result;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
                _logger.LogDebug($"Raw response: {text}");
            }

            return null;
        }

        private static string BuildRequestUrl(string endpoint) => $"https://{Resources.Listenbrainz.BaseUrl}/{Resources.Listenbrainz.ApiVersion}/{endpoint}";

        private void LogRequest(HttpRequestMessage requestMessage)
        {
            var requestData = requestMessage.Content?.ReadAsStringAsync();
            var token = _httpClient.DefaultRequestHeaders.Authorization?.Parameter;
            var obfuscatedToken = $"***{token?[^4..]}";
            _logger.LogDebug("Sending request:");
            _logger.LogDebug("URI: {Uri}", requestMessage.RequestUri);
            _logger.LogDebug("Method: {Method}", requestMessage.Method);
            _logger.LogDebug("Authorization: {Auth}", obfuscatedToken);
            _logger.LogDebug("Additional headers: {Headers}", requestMessage.Headers);
            _logger.LogDebug("Data: {Data}", requestData?.Result);
        }

        private void LogResponse(HttpResponseMessage responseMessage)
        {
            var responseData = responseMessage.Content.ReadAsStringAsync();
            _logger.LogDebug("Got response:");
            _logger.LogDebug("Status: {Status}", responseMessage.StatusCode);
            _logger.LogDebug("Data: {Date}", responseData.Result);
        }
    }
}
