using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Interfaces;
using Jellyfin.Plugin.Listenbrainz.MusicBrainz.Json;
using Jellyfin.Plugin.Listenbrainz.MusicBrainz.Resources;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainz;

/// <summary>
/// Base MusicBrainz API client.
/// </summary>
public class BaseClient : Http.Client
{
    /// <summary>
    /// Serializer options.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = KebabCaseNamingPolicy.Instance
    };

    private readonly string _baseUrl;
    private readonly string _clientName;
    private readonly string _clientVersion;
    private readonly string _clientLink;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClient"/> class.
    /// </summary>
    /// <param name="baseUrl">API base URL.</param>
    /// <param name="clientName">Name of the client.</param>
    /// <param name="clientVersion">Client version.</param>
    /// <param name="clientLink">Link to the client.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    protected BaseClient(
        string baseUrl,
        string clientName,
        string clientVersion,
        string clientLink,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService? sleepService) : base(httpClientFactory, logger, sleepService)
    {
        _baseUrl = baseUrl;
        _clientName = clientName;
        _clientVersion = clientVersion;
        _clientLink = clientLink;
    }

    /// <summary>
    /// Send a GET request to the MusicBrainz server.
    /// </summary>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <typeparam name="TRequest">Data type of the request.</typeparam>
    /// <typeparam name="TResponse">Data type of the response.</typeparam>
    /// <returns>Request response. Null if error.</returns>
    protected async Task<TResponse?> Get<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IMusicBrainzRequest
        where TResponse : IMusicBrainzResponse
    {
        var query = ToMusicbrainzQuery(request.SearchQuery);
        var requestUri = BuildRequestUri(request.Endpoint);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{requestUri}?query={query}")
        };

        var productValue = new ProductInfoHeaderValue(_clientName, _clientVersion);
        var commentValue = new ProductInfoHeaderValue($"(+{_clientLink})");
        var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");

        requestMessage.Headers.UserAgent.Add(productValue);
        requestMessage.Headers.UserAgent.Add(commentValue);
        requestMessage.Headers.Accept.Add(acceptHeader);
        using (requestMessage) return await DoRequest<TResponse>(requestMessage, cancellationToken);
    }

    private Uri BuildRequestUri(string endpoint) => new($"{_baseUrl}/ws/{Api.Version}/{endpoint}");

    private async Task<TResponse?> DoRequest<TResponse>(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        where TResponse : IMusicBrainzResponse
    {
        var response = await SendRequest(requestMessage, cancellationToken);
        var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, SerializerOptions, cancellationToken);
        if (result is null) throw new InvalidResponseException("Response deserialized to NULL");
        return result;
    }

    /// <summary>
    /// Convert dictionary to Musicbrainz query.
    /// </summary>
    /// <param name="requestData">Query data.</param>
    /// <returns>Query string.</returns>
    private static string ToMusicbrainzQuery(Dictionary<string, string> requestData)
    {
        var query = string.Empty;
        int i = 0;
        foreach (var d in requestData)
        {
            query += HttpUtility.UrlEncode($"{d.Key}:{d.Value}");
            if (++i != requestData.Count)
            {
                query += HttpUtility.UrlEncode(" AND ");
            }
        }

        return query;
    }
}
