using System.Globalization;
using System.Reflection;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Handlers;
using Jellyfin.Plugin.ListenBrainz.Managers;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using ClientUtils = Jellyfin.Plugin.ListenBrainz.Clients.Utils;

namespace Jellyfin.Plugin.ListenBrainz;

/// <summary>
/// ListenBrainz Plugin definition for Jellyfin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages, IDisposable
{
    private static Plugin? _thisInstance;
    private readonly ILogger<Plugin> _logger;
    private readonly ISessionManager _sessionManager;
    private readonly IUserDataManager _userDataManager;
    private readonly GenericHandler<PlaybackProgressEventArgs> _playbackStartHandler;
    private readonly GenericHandler<PlaybackStopEventArgs> _playbackStopHandler;
    private readonly GenericHandler<UserDataSaveEventArgs> _userDataSaveHandler;
    private bool _isDisposed;
    private bool _isRegistered;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="paths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    /// <param name="sessionManager">Session manager.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="userDataManager">User data manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    public Plugin(
        IApplicationPaths paths,
        IXmlSerializer xmlSerializer,
        ISessionManager sessionManager,
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        IUserDataManager userDataManager,
        ILibraryManager libraryManager,
        IUserManager userManager) : base(paths, xmlSerializer)
    {
        _thisInstance = this;
        _logger = loggerFactory.CreateLogger<Plugin>();
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;

        var pluginConfigService = new DefaultPluginConfigService();
        var pluginImplLogger = loggerFactory.CreateLogger(LoggerCategory);

        var listenBrainzLogger = loggerFactory.CreateLogger(LoggerCategory + ".ListenBrainzApi");
        var listenBrainzClient = ClientUtils.GetListenBrainzClient(listenBrainzLogger, clientFactory);

        var listenBrainzApiClient = ClientUtils.GetListenBrainzApiClient(listenBrainzLogger, clientFactory);
        var listenBrainzService = new DefaultListenBrainzService(
            pluginImplLogger,
            listenBrainzApiClient,
            pluginConfigService);

        var musicBrainzLogger = loggerFactory.CreateLogger(LoggerCategory + ".MusicBrainzApi");
        var musicBrainzClient = ClientUtils.GetMusicBrainzClient(musicBrainzLogger, clientFactory);

        var clientName = string.Join(string.Empty, Plugin.FullName.Split(' ').Select(s => s.Capitalize()));
        var apiClient = new MusicBrainzApiClient(clientName, Plugin.Version, Plugin.SourceUrl, clientFactory, musicBrainzLogger);

        var metadataProviderLogger = loggerFactory.CreateLogger(LoggerCategory + ".MetadataProvider");
        var metadataProviderService = new DefaultMetadataProviderService(
            metadataProviderLogger,
            apiClient,
            new DefaultPluginConfigService());

        var backupLogger = loggerFactory.CreateLogger(LoggerCategory + ".Backup");
        var backupManager = new BackupManager(backupLogger);

        var favoriteSyncLogger = loggerFactory.CreateLogger(LoggerCategory + ".FavoriteSync");
        var favoriteSyncService = new DefaultFavoriteSyncService(
            favoriteSyncLogger,
            listenBrainzClient,
            musicBrainzClient,
            pluginConfigService,
            libraryManager,
            userManager,
            userDataManager);

        var validationLogger = loggerFactory.CreateLogger(LoggerCategory + ".Validation");
        var validationService = new DefaultValidationService(
            validationLogger,
            pluginConfigService,
            libraryManager);

        _playbackStartHandler = new PlaybackStartHandler(
            pluginImplLogger,
            validationService,
            pluginConfigService,
            metadataProviderService,
            listenBrainzService,
            PlaybackTrackingManager.Instance,
            userManager);

        _playbackStopHandler = new PlaybackStopHandler(
            pluginImplLogger,
            userManager,
            pluginConfigService,
            favoriteSyncService,
            validationService,
            metadataProviderService,
            backupManager,
            listenBrainzClient,
            ListensCacheManager.Instance);

        _userDataSaveHandler = new UserDataSaveHandler(
            pluginImplLogger,
            userManager,
            pluginConfigService,
            favoriteSyncService,
            validationService,
            metadataProviderService,
            backupManager,
            listenBrainzClient,
            ListensCacheManager.Instance,
            PlaybackTrackingManager.Instance);

        RegisterEventHandlers();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="Plugin"/> class.
    /// </summary>
    ~Plugin() => Dispose(false);

    /// <inheritdoc />
    public override string Name => "ListenBrainz";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("59B20823-AAFE-454C-A393-17427F518631");

    /// <summary>
    /// Gets plugin version.
    /// </summary>
    public static new string Version
    {
        get => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
    }

    /// <summary>
    /// Gets full plugin name.
    /// </summary>
    public static string FullName => "ListenBrainz plugin for Jellyfin";

    /// <summary>
    /// Gets plugin source URL.
    /// </summary>
    public static string SourceUrl => "https://github.com/lyarenei/jellyfin-plugin-listenbrainz";

    /// <summary>
    /// Gets logger category.
    /// </summary>
    public static string LoggerCategory => "Jellyfin.Plugin.ListenBrainz";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configurationPage.html",
                EnableInMainMenu = false,
                MenuIcon = "music_note",
            },
        ];
    }

    /// <summary>
    /// Convenience method for getting plugin configuration.
    /// </summary>
    /// <returns>Plugin configuration.</returns>
    /// <exception cref="PluginException">Plugin instance is not available.</exception>
    [Obsolete("Use PluginConfigurationService to access plugin configuration.")]
    public static PluginConfiguration GetConfiguration()
    {
        var config = _thisInstance?.Configuration;
        if (config is not null)
        {
            return config;
        }

        throw new PluginException("Plugin instance is not available");
    }

    /// <summary>
    /// Gets plugin data path.
    /// </summary>
    /// <returns>Path to the plugin data folder.</returns>
    /// <exception cref="PluginException">Plugin instance is not available.</exception>
    public static string GetDataPath()
    {
        // DataFolderPath is invalid (https://github.com/jellyfin/jellyfin/issues/10091)
        // var path = _thisInstance?.DataFolderPath;
        var pluginDirName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", _thisInstance?.Name, Version);
        var path = Path.Join(_thisInstance?.ApplicationPaths.PluginsPath, pluginDirName);
        if (path is null)
        {
            throw new PluginException("Plugin instance is not available");
        }

        return path;
    }

    /// <summary>
    /// Gets plugin configuration directory path.
    /// </summary>
    /// <returns>Path to config directory.</returns>
    /// <exception cref="PluginException">Plugin instance or path is not available.</exception>
    public static string GetConfigDirPath()
    {
        var path = _thisInstance?.ConfigurationFilePath;
        if (path is null)
        {
            throw new PluginException("Plugin instance is not available");
        }

        var dirName = Path.GetDirectoryName(path);
        if (dirName is null)
        {
            throw new PluginException("Could not get a config directory name");
        }

        return dirName;
    }

    /// <summary>
    /// Update plugin configuration.
    /// </summary>
    /// <param name="newConfiguration">New plugin configuration.</param>
    public static void UpdateConfig(BasePluginConfiguration newConfiguration)
    {
        _thisInstance?.UpdateConfiguration(newConfiguration);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose unmanaged (own) and optionally managed resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        UnregisterEventHandlers();

        if (disposing)
        {
            // Dispose managed resources here.
        }

        _isDisposed = true;
    }

    /// <summary>
    /// Register event handlers for the plugin.
    /// </summary>
    private void RegisterEventHandlers()
    {
        if (_isRegistered)
        {
            _logger.LogDebug("Plugin event handlers are already registered");
            return;
        }

        _sessionManager.PlaybackStart += _playbackStartHandler.HandleEvent;
        _sessionManager.PlaybackStopped += _playbackStopHandler.HandleEvent;
        _userDataManager.UserDataSaved += _userDataSaveHandler.HandleEvent;

        _isRegistered = true;
        _logger.LogDebug("Plugin event handlers have been registered");
    }

    /// <summary>
    /// Unregister event handlers for the plugin.
    /// </summary>
    private void UnregisterEventHandlers()
    {
        if (!_isRegistered)
        {
            _logger.LogDebug("Plugin event handlers are already unregistered");
            return;
        }

        _sessionManager.PlaybackStart -= _playbackStartHandler.HandleEvent;
        _sessionManager.PlaybackStopped -= _playbackStopHandler.HandleEvent;
        _userDataManager.UserDataSaved -= _userDataSaveHandler.HandleEvent;

        _isRegistered = false;
        _logger.LogDebug("Plugin event handlers have been unregistered");
    }
}
