using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Resources;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Api
{
    public class BaseMbClient
    {
        private const string Version = "2";
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public BaseMbClient(IHttpClientFactory httpClientFactory, ILogger logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
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
            try
            {
                return new BaseResponse(XElement.Load(stream)) as TResponse;
            }
            catch (Exception e)
            {
                _logger.LogDebug(e.Message);
            }

            return null;
        }

        private static string BuildRequestUrl(string endpoint) => $"https://{Musicbrainz.BaseUrl}/ws/{Version}/{endpoint}";
    }
}
