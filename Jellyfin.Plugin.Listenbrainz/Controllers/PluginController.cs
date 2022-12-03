using System.Net.Http;
using Jellyfin.Plugin.Listenbrainz.Clients;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Controllers
{
    /// <summary>
    /// Plugin controller.
    /// </summary>
    [ApiController]
    [Route("Listenbrainz/ValidateToken")]
    public class PluginController : ControllerBase
    {
        private readonly ListenbrainzClient _apiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginController"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="loggerFactory">Logger instance.</param>
        public PluginController(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<PluginController>();
            var baseUrl = Plugin.Instance?.Configuration.GlobalConfig.ListenbrainzBaseUrl ?? Api.BaseUrl;
            _apiClient = new ListenbrainzClient(baseUrl, httpClientFactory, logger, new SleepService());
        }

        /// <summary>
        /// Verify Listenbrainz API token.
        /// </summary>
        /// <param name="token">API token.</param>
        /// <returns><see cref="ValidateTokenResponse"/> as object.</returns>
        [HttpPost]
        [Consumes("application/json")]
        public object? VerifyToken([FromBody] string token)
        {
            return _apiClient.ValidateToken(token).Result;
        }
    }
}
