using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of a PluginConfig service.
/// </summary>
public class DefaultPluginConfigService : IPluginConfigService
{
    /// <inheritdoc />
    public UserConfig? GetUserConfig(Guid jellyfinUserId)
    {
        var userConfig = Plugin
            .GetConfiguration()
            .UserConfigs
            .FirstOrDefault(u => u.JellyfinUserId == jellyfinUserId);

        return userConfig;
    }

    /// <inheritdoc />
    public string GetListenBrainzApiUrl() => Plugin.GetConfiguration().ListenBrainzApiUrl;
}
