using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Json;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients
{
    /// <summary>
    /// Base Listenbrainz API client.
    /// </summary>
    public class BaseListenbrainzClient : BaseHttpClient
    {
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _serOpts;
        private readonly string _baseUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseListenbrainzClient"/> class.
        /// </summary>
        /// <param name="baseUrl">API base URL.</param>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="sleepService">Sleep service.</param>
        public BaseListenbrainzClient(
            string baseUrl,
            IHttpClientFactory httpClientFactory,
            ILogger logger,
            ISleepService sleepService) : base(httpClientFactory, logger, sleepService)
        {
            _baseUrl = baseUrl;
            _logger = logger;
            _serOpts = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
            };
        }

        /// <summary>
        /// Send a POST request to the Listenbrainz server.
        /// </summary>
        /// <typeparam name="TRequest">Data type of the request.</typeparam>
        /// <typeparam name="TResponse">Data type of the response.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <returns>Request response. Null if error.</returns>
        protected async Task<TResponse?> Post<TRequest, TResponse>(TRequest request)
            where TRequest : BaseRequest
            where TResponse : BaseResponse
        {
            var jsonData = JsonSerializer.Serialize(request.ToRequestForm(), _serOpts);
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = BuildRequestUrl(_baseUrl, request.GetEndpoint()),
                Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
            };

            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("token", request.ApiToken);
            using (requestMessage)
            {
                return await DoRequest<TResponse>(requestMessage).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Send a GET request to the Listenbrainz server.
        /// </summary>
        /// <typeparam name="TRequest">Data type of the request.</typeparam>
        /// <typeparam name="TResponse">Data type of the response.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <returns>Request response. Null if error.</returns>
        protected async Task<TResponse?> Get<TRequest, TResponse>(TRequest request)
            where TRequest : BaseRequest
            where TResponse : BaseResponse
        {
            var query = ToHttpGetQuery(request.ToRequestForm());
            var url = BuildRequestUrl(_baseUrl, request.GetEndpoint());
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{url}?{query}")
            };
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("token", request.ApiToken);

            using (requestMessage)
            {
                return await DoRequest<TResponse>(requestMessage).ConfigureAwait(false);
            }
        }

        private async Task<TResponse?> DoRequest<TResponse>(HttpRequestMessage requestMessage)
            where TResponse : BaseResponse
        {
            HttpResponseMessage? response;
            try
            {
                response = await SendRequest(requestMessage).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is RetryException or InvalidResponseException)
            {
                return null;
            }

            var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            try
            {
                var result = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, _serOpts).ConfigureAwait(true);
                if (result == null)
                {
                    _logger.LogDebug("Response deserialized to null");
                    return null;
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogDebug("Exception while trying to deserialize response: {Exception}", e.Message);
            }

            return null;
        }

        private Uri BuildRequestUrl(string baseUrl, string endpoint)
        {
            return new Uri($"{baseUrl}/{Api.Version}/{endpoint}");
        }

        /// <summary>
        /// Convert dictionary to HTTP GET query.
        /// </summary>
        /// <param name="requestData">Query data.</param>
        /// <returns>Query string.</returns>
        private string ToHttpGetQuery(object requestData)
        {
            Dictionary<string, string> reqData;
            try
            {
                reqData = (Dictionary<string, string>)requestData;
            }
            catch (InvalidCastException)
            {
               _logger.LogDebug("Failed to cast request data to Dict");
               return string.Empty;
            }

            var query = string.Empty;
            int i = 0;
            foreach (var d in reqData)
            {
                query += $"{d.Key}={d.Value}";
                if (++i != reqData.Count)
                {
                    query += '&';
                }
            }

            return query;
        }
    }
}
