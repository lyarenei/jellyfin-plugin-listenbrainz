using System.Net.Http;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Api
{
    [ApiController]
    [Route("Listenbrainz/ValidateToken")]
    public class RestApi : ControllerBase
    {
        private readonly LbApiClient _apiClient;
        private readonly ILogger<RestApi> _logger;

        public RestApi(IJsonSerializer jsonSerializer, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RestApi>();
            _apiClient = new LbApiClient(httpClientFactory, jsonSerializer, _logger);
        }

        [HttpPost]
        [Consumes("application/json")]
        public object VerifyToken([FromBody] string token)
        {
            _logger.LogInformation($"Verifying token '{token}'");
            return _apiClient.ValidateToken(token).Result;
        }
    }
}
