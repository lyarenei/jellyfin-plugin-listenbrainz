using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Resources;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Api
{
    public class BaseMbClient
    {
        private const string Version = "2";
        private readonly HttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ILogger _logger;
        public BaseMbClient(IHttpClientFactory httpClientFactory, IJsonSerializer jsonSerializer, ILogger logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _jsonSerializer = jsonSerializer;
            _logger = logger;

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var productValue = new ProductInfoHeaderValue("JellyfinListenbrainzPlugin", version);
            var commentValue = new ProductInfoHeaderValue("(+https://github.com/lyarenei/jellyfin-plugin-listenbrainz)");
            var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");

            _httpClient.DefaultRequestHeaders.UserAgent.Add(productValue);
            _httpClient.DefaultRequestHeaders.UserAgent.Add(commentValue);
            _httpClient.DefaultRequestHeaders.Accept.Add(acceptHeader);
        }

        public async Task<TResponse> Get<TRequest, TResponse>(TRequest request) where TRequest : BaseRequest where TResponse : BaseResponse
        {
            return await Get<TRequest, TResponse>(request, CancellationToken.None);
        }

        public async Task<TResponse> Get<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken) where TRequest : BaseRequest where TResponse : BaseResponse
        {
            var requestData = request.ToRequestForm();
            string url = $"{BuildRequestUrl(request.GetEndpoint())}?query=";

            int i = 0;
            foreach (var d in requestData)
            {
                url += HttpUtility.UrlEncode($"{d.Key}:{d.Value}");
                if (++i != requestData.Count)
                    url += HttpUtility.UrlEncode(" AND ");
            }

            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };

            using var response = await _httpClient.SendAsync(requestMessage, CancellationToken.None);
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
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
                        _logger.LogError(result.Error);
                    }

                    return result;
                }
                catch (Exception e)
                {
                    _logger.LogDebug(e.Message);
                }
            }

            return null;
        }

        private static string BuildRequestUrl(string endpoint) => $"https://{Musicbrainz.BaseUrl}/ws/{Version}/{endpoint}";
    }
}
