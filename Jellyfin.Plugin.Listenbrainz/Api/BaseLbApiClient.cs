using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;

namespace Jellyfin.Plugin.Listenbrainz.Api
{
    public class BaseLbApiClient
    {
        private const string ApiVersion = "1";

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;


        public BaseLbApiClient(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient();
            _jsonSerializer = jsonSerializer;
            _logger = logger;
        }

        /// <summary>
        /// Send a POST request to the Listenbrainz Api
        /// </summary>
        /// <typeparam name="TRequest">The type of the request</typeparam>
        /// <typeparam name="TResponse">The type of the response</typeparam>
        /// <param name="request">The request</param>
        /// <returns>A response with type TResponse</returns>
        public async Task<TResponse> Post<TRequest, TResponse>(TRequest request) where TRequest : BaseRequest where TResponse : BaseResponse
        {
            var jsonData = _jsonSerializer.SerializeToString(request.ToRequestForm());
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(BuildRequestUrl(request.GetEndpoint())),
                Content = new StringContent(jsonData, Encoding.UTF8, "application/json")
            };


            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", request.ApiToken);
            using var response = await _httpClient.SendAsync(requestMessage, CancellationToken.None);
            using (var stream = await response.Content.ReadAsStreamAsync())
            {
                try
                {
                    var result = _jsonSerializer.DeserializeFromStream<TResponse>(stream);
                    if (result.IsError())
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        StreamReader reader = new StreamReader(stream);
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

            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };

            using var response = await _httpClient.SendAsync(requestMessage, CancellationToken.None);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            try
            {
                var result = _jsonSerializer.DeserializeFromStream<TResponse>(stream);
                if (result.IsError())
                    _logger.LogError(result.Message);

                return result;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
            }

            return null;
        }

        private static string BuildRequestUrl(string endpoint) => $"https://{Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz.BaseUrl}/{ApiVersion}/{endpoint}";
    }
}
