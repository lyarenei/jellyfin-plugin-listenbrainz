using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// A service factory interface.
/// </summary>
public interface IServiceFactory
{
    /// <summary>
    /// Get a ListenBrainz service.
    /// </summary>
    /// <returns>ListenBrainz service.</returns>
    public IListenBrainzService GetListenBrainzService();

    /// <summary>
    /// Get a Metadata provider service.
    /// </summary>
    /// <returns>Metadata provider service.</returns>
    public IMetadataProviderService GetMetadataProviderService();

    /// <summary>
    /// Get a Favorite sync service.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="userManager">Jellyfin user manager.</param>
    /// <param name="userDataManager">Jellyfin user data manager.</param>
    /// <param name="listenBrainzService">ListenBrainz service.</param>
    /// <param name="metadataProviderService">Metadata provider service.</param>
    /// <param name="pluginConfigService">Plugin configuration service.</param>
    /// <returns>Favorite sync service.</returns>
    public IFavoriteSyncService GetFavoriteSyncService(
        ILibraryManager libraryManager,
        IUserManager userManager,
        IUserDataManager userDataManager,
        IListenBrainzService? listenBrainzService,
        IMetadataProviderService? metadataProviderService,
        IPluginConfigService? pluginConfigService);

    /// <summary>
    /// Get a Plugin configuration service.
    /// </summary>
    /// <returns>Plugin configuration service.</returns>
    public IPluginConfigService GetPluginConfigService();

    /// <summary>
    /// Get a default Validation service.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <param name="pluginConfigService">Plugin configuration service.</param>
    /// <returns>Validation service.</returns>
    public IValidationService GetValidationService(
        ILibraryManager libraryManager,
        IPluginConfigService? pluginConfigService);
}
