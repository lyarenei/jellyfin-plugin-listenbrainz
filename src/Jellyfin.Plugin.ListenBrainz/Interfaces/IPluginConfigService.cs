using Jellyfin.Plugin.ListenBrainz.Configuration;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// A service for accessing plugin configuration.
/// </summary>
public interface IPluginConfigService
{
    /// <summary>
    /// Get a configuration for a specified Jellyfin user ID.
    /// </summary>
    /// <param name="jellyfinUserId">ID of the Jellyfin user.</param>
    /// <returns>User configuration. Null if it does not exist.</returns>
    public UserConfig? GetUserConfig(Guid jellyfinUserId);

    /// <summary>
    /// Get configured ListenBrainz API URL.
    /// </summary>
    /// <returns>ListenBrainz API URL.</returns>
    public string GetListenBrainzApiUrl();
}
