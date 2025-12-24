using Jellyfin.Plugin.ListenBrainz.Api;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using UnderlyingClient = Jellyfin.Plugin.ListenBrainz.Http.HttpClient;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Service factory.
/// </summary>
public class ServiceFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceFactory"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    public ServiceFactory(ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    private static string LoggerCategory => "Jellyfin.Plugin.ListenBrainz";

    /// <summary>
    /// Get a default ListenBrainz service.
    /// </summary>
    /// <returns>ListenBrainz service.</returns>
    public IListenBrainzService GetDefaultListenBrainzService()
    {
        var httpLogger = _loggerFactory.CreateLogger(LoggerCategory + ".HttpClient");
        var apiLogger = _loggerFactory.CreateLogger(LoggerCategory + ".Api");
        var serviceLogger = _loggerFactory.CreateLogger(LoggerCategory);

        var httpClient = new UnderlyingClient(_httpClientFactory, httpLogger, null);
        var wrapper = new HttpClientWrapper(httpClient);
        using var baseClient = new BaseApiClient(wrapper, apiLogger, null);
        var apiClient = new ListenBrainzApiClient(baseClient, apiLogger);

        var pluginConfig = GetDefaultPluginConfigService();

        return new DefaultListenBrainzService(serviceLogger, apiClient, pluginConfig);
    }

    /// <summary>
    /// Get a default Metadata provider service.
    /// </summary>
    /// <returns>Metadata provider service.</returns>
    public IMetadataProviderService GetDefaultMetadataProviderService()
    {
        var apiLogger = _loggerFactory.CreateLogger(LoggerCategory + ".MusicBrainzApi");
        var serviceLogger = _loggerFactory.CreateLogger(LoggerCategory + ".MetadataProvider");

        var clientName = string.Join(string.Empty, Plugin.FullName.Split(' ').Select(s => s.Capitalize()));
        using var apiClient = new MusicBrainzApiClient(
            clientName,
            Plugin.Version,
            Plugin.SourceUrl,
            _httpClientFactory,
            apiLogger);

        var pluginConfig = GetDefaultPluginConfigService();

        return new DefaultMetadataProviderService(serviceLogger, apiClient, pluginConfig);
    }

    /// <summary>
    /// Get a default Favorite sync service.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="userDataManager">Jellyfin user data manager.</param>
    /// <param name="listenBrainzService">ListenBrainz service.</param>
    /// <param name="metadataProviderService">Metadata provider service.</param>
    /// <param name="pluginConfigService">Plugin configuration service.</param>
    /// <returns>Favorite sync service.</returns>
    public IFavoriteSyncService GetDefaultFavoriteSyncService(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        IListenBrainzService? listenBrainzService,
        IMetadataProviderService? metadataProviderService,
        IPluginConfigService? pluginConfigService)
    {
        var serviceLogger = _loggerFactory.CreateLogger(LoggerCategory + ".FavoriteSync");

        var listenBrainz = listenBrainzService ?? GetDefaultListenBrainzService();
        var metadataProvider = metadataProviderService ?? GetDefaultMetadataProviderService();
        var pluginConfig = pluginConfigService ?? GetDefaultPluginConfigService();

        return new DefaultFavoriteSyncService(
            serviceLogger,
            listenBrainz,
            metadataProvider,
            pluginConfig,
            libraryManager,
            userManager,
            userDataManager);
    }

    /// <summary>
    /// Get a default Plugin configuration service.
    /// </summary>
    /// <returns>Plugin configuration service.</returns>
    public IPluginConfigService GetDefaultPluginConfigService()
    {
        return new DefaultPluginConfigService();
    }

    /// <summary>
    /// Get a default Validation service.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="pluginConfigService">Plugin configuration service.</param>
    /// <returns>Validation service.</returns>
    public IValidationService GetDefaultValidationService(
        ILibraryManager libraryManager,
        IPluginConfigService? pluginConfigService)
    {
        var validationLogger = _loggerFactory.CreateLogger(LoggerCategory + ".Validation");

        var pluginConfig = pluginConfigService ?? GetDefaultPluginConfigService();

        return new DefaultValidationService(validationLogger, pluginConfig, libraryManager);
    }
}
