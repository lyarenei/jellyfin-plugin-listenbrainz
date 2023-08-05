using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Jellyfin.Plugin.ListenBrainz.Http.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Http.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Json;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Resources;
using Microsoft.Extensions.Logging;
using HttpClient = Jellyfin.Plugin.ListenBrainz.Http.HttpClient;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;

/// <summary>
/// Base MusicBrainz API client.
/// </summary>
public class BaseClient : HttpClient
{
    /// <summary>
    /// Serializer options.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = KebabCaseNamingPolicy.Instance
    };

    private readonly string _clientName;
    private readonly string _clientVersion;
    private readonly string _contactUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseClient"/> class.
    /// </summary>
    /// <param name="clientName">Name of the client.</param>
    /// <param name="clientVersion">Client version.</param>
    /// <param name="contactUrl">Link to the client.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="sleepService">Sleep service.</param>
    protected BaseClient(
        string clientName,
        string clientVersion,
        string contactUrl,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        ISleepService? sleepService) : base(httpClientFactory, logger, sleepService)
    {
        _clientName = clientName;
        _clientVersion = clientVersion;
        _contactUrl = contactUrl;
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
        var requestUri = BuildRequestUri(request.BaseUrl, request.Endpoint);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri($"{requestUri}?query={query}&limit=1")
        };

        var productValue = new ProductInfoHeaderValue(_clientName, _clientVersion);
        var commentValue = new ProductInfoHeaderValue($"( {_contactUrl} )");
        var acceptHeader = new MediaTypeWithQualityHeaderValue("application/json");

        requestMessage.Headers.UserAgent.Add(productValue);
        requestMessage.Headers.UserAgent.Add(commentValue);
        requestMessage.Headers.Accept.Add(acceptHeader);
        using (requestMessage) return await DoRequest<TResponse>(requestMessage, cancellationToken);
    }

    private Uri BuildRequestUri(string baseUrl, string endpoint) => new($"{baseUrl}/ws/{Api.Version}/{endpoint}");

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
        var i = 0;
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
