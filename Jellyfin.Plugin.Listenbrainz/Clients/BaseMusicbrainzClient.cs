using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Json;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Resources.Musicbrainz;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Clients
{
    /// <summary>
    /// Base Musicbrainz API client.
    /// </summary>
    public class BaseMusicbrainzClient : BaseHttpClient
    {
        private readonly ILogger _logger;
        private readonly JsonSerializerOptions _serOpts;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMusicbrainzClient"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="sleepService">Sleep service.</param>
        public BaseMusicbrainzClient(
            IHttpClientFactory httpClientFactory,
            ILogger logger,
            ISleepService sleepService) : base(httpClientFactory, logger, sleepService)
        {
            _logger = logger;
            _serOpts = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = KebabCaseNamingPolicy.Instance
            };
        }

        /// <summary>
        /// Send a GET request to the Musicbrainz server.
        /// </summary>
        /// <typeparam name="TRequest">Data type of the request.</typeparam>
        /// <typeparam name="TResponse">Data type of the response.</typeparam>
        /// <param name="request">The request to send.</param>
        /// <returns>Request response. Null if error.</returns>
        public async Task<TResponse?> Get<TRequest, TResponse>(TRequest request)
            where TRequest : BaseRequest
            where TResponse : BaseResponse
        {
            var query = ToMusicbrainzQuery(request.ToRequestForm());
            var requestUri = BuildRequestUri(request.GetEndpoint());
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{requestUri}?query={query}")
            };

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var productValue = new ProductInfoHeaderValue("JellyfinListenbrainzPlugin", version);
            var commentValue = new ProductInfoHeaderValue("(+https://github.com/lyarenei/jellyfin-plugin-listenbrainz)");
            var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");

            requestMessage.Headers.UserAgent.Add(productValue);
            requestMessage.Headers.UserAgent.Add(commentValue);
            requestMessage.Headers.Accept.Add(acceptHeader);
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

        private static Uri BuildRequestUri(string endpoint) => new($"{Api.BaseUrl}/ws/{Api.Version}/{endpoint}");

        /// <summary>
        /// Convert dictionary to Musicbrainz query.
        /// </summary>
        /// <param name="requestData">Query data.</param>
        /// <returns>Query string.</returns>
        private string ToMusicbrainzQuery(object requestData)
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
                query += HttpUtility.UrlEncode($"{d.Key}:{d.Value}");
                if (++i != reqData.Count)
                {
                    query += HttpUtility.UrlEncode(" AND ");
                }
            }

            return query;
        }
    }
}
