using Jellyfin.Plugin.ListenBrainz.Api;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;
using Jellyfin.Plugin.ListenBrainz.Services;
using Microsoft.Extensions.Logging;
using UnderlyingClient = Jellyfin.Plugin.ListenBrainz.Http.HttpClient;

namespace Jellyfin.Plugin.ListenBrainz.Clients;

/// <summary>
/// Client utils.
/// </summary>
public static class Utils
{
    /// <summary>
    /// Get a ListenBrainz client.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <returns>ListenBrainz client.</returns>
    public static IListenBrainzClient GetListenBrainzClient(
        ILogger logger,
        IHttpClientFactory clientFactory)
    {
        var httpClient = new UnderlyingClient(clientFactory, logger, null);
        var baseClient = new BaseApiClient(new HttpClientWrapper(httpClient), logger, null);
        var apiClient = new ListenBrainzApiClient(baseClient, logger);
        var pluginConfig = new DefaultPluginConfigService();
        return new ListenBrainzClient(logger, apiClient, pluginConfig);
    }

    /// <summary>
    /// Get a ListenBrainz API client.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <returns>ListenBrainz client.</returns>
    public static IListenBrainzApiClient GetListenBrainzApiClient(
        ILogger logger,
        IHttpClientFactory clientFactory)
    {
        var httpClient = new UnderlyingClient(clientFactory, logger, null);
        var baseClient = new BaseApiClient(new HttpClientWrapper(httpClient), logger, null);
        return new ListenBrainzApiClient(baseClient, logger);
    }

    /// <summary>
    /// Get a MusicBrainz client.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <returns>Instance of <see cref="IMusicBrainzClient"/>.</returns>
    public static IMusicBrainzClient GetMusicBrainzClient(ILogger logger, IHttpClientFactory clientFactory)
    {
        var clientName = string.Join(string.Empty, Plugin.FullName.Split(' ').Select(s => s.Capitalize()));
        var apiClient = new MusicBrainzApiClient(clientName, Plugin.Version, Plugin.SourceUrl, clientFactory, logger);
        var pluginConfig = new DefaultPluginConfigService();
        return new MusicBrainzClient(logger, apiClient, pluginConfig);
    }
}
