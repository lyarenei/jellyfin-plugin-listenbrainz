using System;
using System.Net.Http;
using Jellyfin.Plugin.Listenbrainz.Clients.ListenBrainz;
using Jellyfin.Plugin.Listenbrainz.Configuration;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses;
using Jellyfin.Plugin.Listenbrainz.Resources.ListenBrainz;
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
        private readonly ListenBrainzClient _apiClient;
        private GlobalConfiguration _pluginConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginController"/> class.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="loggerFactory">Logger instance.</param>
        public PluginController(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
        {
            _pluginConfig = Plugin.Instance?.Configuration.GlobalConfig ?? throw new InvalidOperationException("plugin configuration is NULL");
            var logger = loggerFactory.CreateLogger<PluginController>();
            var baseUrl = _pluginConfig.ListenbrainzBaseUrl ?? Api.BaseUrl;
            _apiClient = new ListenBrainzClient(baseUrl, httpClientFactory, logger, new SleepService());
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
