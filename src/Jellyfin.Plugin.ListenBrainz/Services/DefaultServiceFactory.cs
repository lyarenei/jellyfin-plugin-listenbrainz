using Jellyfin.Plugin.ListenBrainz.Api;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using UnderlyingClient = Jellyfin.Plugin.ListenBrainz.Http.HttpClient;

namespace Jellyfin.Plugin.ListenBrainz.Services;

using ListenCacheData = System.Collections.Generic.Dictionary<
    System.Guid,
    System.Collections.Generic.List<
        Jellyfin.Plugin.ListenBrainz.Dtos.StoredListen
    >
>;

/// <summary>
/// A default implementation of <see cref="IServiceFactory"/>.
/// </summary>
public class DefaultServiceFactory : IServiceFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultServiceFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    public DefaultServiceFactory(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    private static string LoggerCategory => "Jellyfin.Plugin.ListenBrainz";

    /// <inheritdoc />
    public IListenBrainzService GetListenBrainzService()
    {
        var httpLogger = _loggerFactory.CreateLogger(LoggerCategory + ".HttpClient");
        var apiLogger = _loggerFactory.CreateLogger(LoggerCategory + ".Api");
        var serviceLogger = _loggerFactory.CreateLogger(LoggerCategory);

        var httpClient = new UnderlyingClient(_httpClientFactory, httpLogger, null);
        var wrapper = new HttpClientWrapper(httpClient);
        var baseClient = new BaseApiClient(wrapper, apiLogger, null);
        var apiClient = new ListenBrainzApiClient(baseClient, apiLogger);

        var pluginConfig = GetPluginConfigService();

        return new DefaultListenBrainzService(serviceLogger, apiClient, pluginConfig);
    }

    /// <inheritdoc />
    public IMetadataProviderService GetMetadataProviderService()
    {
        var apiLogger = _loggerFactory.CreateLogger(LoggerCategory + ".MusicBrainzApi");
        var serviceLogger = _loggerFactory.CreateLogger(LoggerCategory + ".MetadataProvider");

        var clientName = string.Join(string.Empty, Plugin.FullName.Split(' ').Select(s => s.Capitalize()));
        var apiClient = new MusicBrainzApiClient(
            clientName,
            Plugin.Version,
            Plugin.SourceUrl,
            _httpClientFactory,
            apiLogger);

        var pluginConfig = GetPluginConfigService();

        return new DefaultMetadataProviderService(serviceLogger, apiClient, pluginConfig);
    }

    /// <inheritdoc />
    public IFavoriteSyncService GetFavoriteSyncService(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        IListenBrainzService? listenBrainzService,
        IMetadataProviderService? metadataProviderService,
        IPluginConfigService? pluginConfigService)
    {
        var serviceLogger = _loggerFactory.CreateLogger(LoggerCategory + ".FavoriteSync");

        var listenBrainz = listenBrainzService ?? GetListenBrainzService();
        var metadataProvider = metadataProviderService ?? GetMetadataProviderService();
        var pluginConfig = pluginConfigService ?? GetPluginConfigService();

        return new DefaultFavoriteSyncService(
            serviceLogger,
            listenBrainz,
            metadataProvider,
            pluginConfig,
            libraryManager,
            userManager,
            userDataManager);
    }

    /// <inheritdoc />
    public IPluginConfigService GetPluginConfigService()
    {
        return new DefaultPluginConfigService();
    }

    /// <inheritdoc />
    public IValidationService GetValidationService(
        ILibraryManager libraryManager,
        IPluginConfigService? pluginConfigService)
    {
        var validationLogger = _loggerFactory.CreateLogger(LoggerCategory + ".Validation");

        var pluginConfig = pluginConfigService ?? GetPluginConfigService();

        return new DefaultValidationService(validationLogger, pluginConfig, libraryManager);
    }

    /// <inheritdoc />
    public IListensCachingService GetListensCachingService(
        IPersistentJsonService<ListenCacheData>? persistentService = null)
    {
        try
        {
            return DefaultListensCachingService.GetInstance();
        }
        catch (ServiceException)
        {
            var cacheLogger = _loggerFactory.CreateLogger(LoggerCategory + ".ListensCache");
            var filePath = Path.Join(Plugin.GetDataPath(), "cache.json");
            var storage = persistentService ?? new DefaultPersistentJsonService<ListenCacheData>(filePath);
            return new DefaultListensCachingService(cacheLogger, storage);
        }
    }
}
