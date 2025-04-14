using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of a PluginConfig service.
/// </summary>
public class DefaultPluginConfigService : IPluginConfigService
{
    private static PluginConfiguration Config => Plugin.GetConfiguration();

    /// <inheritdoc />
    public bool IsAlternativeModeEnabled
    {
        get => Config.IsAlternativeModeEnabled;
    }

    /// <inheritdoc />
    public string ListenBrainzApiUrl
    {
        get => Config.ListenBrainzApiUrl;
    }

    /// <inheritdoc />
    public UserConfig? GetUserConfig(Guid jellyfinUserId)
    {
        var userConfig = Config
            .UserConfigs
            .FirstOrDefault(u => u.JellyfinUserId == jellyfinUserId);

        return userConfig;
    }
}
