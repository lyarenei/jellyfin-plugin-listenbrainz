using Jellyfin.Plugin.ListenBrainz.Clients;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.ListenBrainzApi;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Utils;

/// <summary>
/// Client utils.
/// </summary>
public static class ClientUtils
{
    /// <summary>
    /// Get a ListenBrainz client.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <returns>ListenBrainz client.</returns>
    public static IListenBrainzClient GetListenBrainzClient(
        ILogger logger,
        IHttpClientFactory clientFactory,
        ILibraryManager? libraryManager = null)
    {
        var apiClient = new ListenBrainzApiClient(clientFactory, logger);
        return libraryManager is null ? new ListenBrainzClient(logger, apiClient) : new ListenBrainzClient(logger, apiClient, libraryManager);
    }

    /// <summary>
    /// Get a MusicBrainz client.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <returns>Instance of <see cref="IMetadataClient"/>.</returns>
    public static IMetadataClient GetMusicBrainzClient(ILogger logger, IHttpClientFactory clientFactory)
    {
        var config = Plugin.GetConfiguration();
        if (!config.IsMusicBrainzEnabled) return new DummyMusicBrainzClient(logger);

        var clientName = string.Join(string.Empty, Plugin.FullName.Split(' ').Select(s => s.Capitalize()));
        var apiClient = new MusicBrainzApiClient(
            clientName,
            Plugin.Version,
            Plugin.SourceUrl,
            clientFactory,
            logger);

        return new MusicBrainzClient(logger, apiClient);
    }
}
