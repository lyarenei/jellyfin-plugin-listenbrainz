using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Controllers;

/// <summary>
/// Controller for serving internal plugin resources.
/// </summary>
[ApiController]
[Route("ListenBrainzPlugin")]
public class PluginController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IListenBrainzService _listenBrainz;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginController"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="listenBrainz">ListenBrainz service.</param>
    public PluginController(ILoggerFactory loggerFactory, IListenBrainzService listenBrainz)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.Controller");
        _listenBrainz = listenBrainz;
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
            return await _listenBrainz.ValidateTokenAsync(apiToken, CancellationToken.None);
        }
        catch (Exception e)
        {
            _logger.LogInformation("Token verification failed: {Reason}", e.Message);
            _logger.LogDebug(e, "Token verification failed");
        }

        return null;
    }
}
