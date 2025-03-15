using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Managers;

/// <summary>
/// Default implementation of <see cref="IPluginConfigManager"/>.
/// </summary>
public class PluginConfigManager : IPluginConfigManager
{
    /// <inheritdoc />
    public PluginConfiguration GetConfiguration()
    {
        return Plugin.GetConfiguration();
    }

    /// <inheritdoc />
    public void SaveConfiguration(PluginConfiguration config)
    {
        Plugin.UpdateConfig(config);
    }
}
