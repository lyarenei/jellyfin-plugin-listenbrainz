using System.Net;
using Jellyfin.Plugin.ListenBrainz.HttpClient.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.HttpClient;

/// <summary>
/// A custom HTTP client.
/// Can be used as a base for application-specific API clients.
/// </summary>
public class Client
{
    private const int RetryBackoffSeconds = 3;
    private const int RetryCount = 6;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly ISleepService _sleepService;

    private readonly List<HttpStatusCode> _retryStatuses = new()
    {
        HttpStatusCode.InternalServerError,
        HttpStatusCode.BadGateway,
        HttpStatusCode.ServiceUnavailable,
        HttpStatusCode.GatewayTimeout,
        HttpStatusCode.InsufficientStorage
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="Client"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    public Client(IHttpClientFactory httpClientFactory, ILogger logger, ISleepService sleepService)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _sleepService = sleepService;
    }
}
