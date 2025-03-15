using Jellyfin.Plugin.ListenBrainz.Configuration;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Plugin configuration manager.
/// </summary>
public interface IPluginConfigManager
{
    /// <summary>
    /// Gets the plugin configuration.
    /// </summary>
    /// <returns>Plugin configuration.</returns>
    PluginConfiguration GetConfiguration();

    /// <summary>
    /// Saves the plugin configuration.
    /// </summary>
    /// <param name="config">Plugin configuration.</param>
    void SaveConfiguration(PluginConfiguration config);
}
