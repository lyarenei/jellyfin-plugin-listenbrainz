using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ClientUtils = Jellyfin.Plugin.ListenBrainz.Clients.Utils;

namespace Jellyfin.Plugin.ListenBrainz.Controllers;

/// <summary>
/// Controller for serving internal plugin resources.
/// </summary>
[ApiController]
[Route("ListenBrainzPlugin")]
public class PluginController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IListenBrainzClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginController"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    public PluginController(ILoggerFactory loggerFactory, IHttpClientFactory clientFactory)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.Controller");
        _client = ClientUtils.GetListenBrainzClient(_logger, clientFactory);
    }

    /// <summary>
    /// Validate ListenBrainz API token.
    /// </summary>
    /// <param name="apiToken">Token to verify.</param>
    /// <returns><see cref="ValidatedToken"/> response.</returns>
    [HttpPost]
    [Route("ValidateToken")]
    [Consumes("application/json")]
    public async Task<ValidatedToken?> ValidateToken([FromBody] string apiToken)
    {
        try
        {
            return await _client.ValidateToken(apiToken);
        }
        catch (Exception e)
        {
            _logger.LogInformation("Token verification failed: {Reason}", e.Message);
            _logger.LogDebug(e, "Token verification failed");
        }

        return null;
    }
}
