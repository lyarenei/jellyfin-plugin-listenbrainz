using Jellyfin.Data.Entities;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Exceptions;

namespace Jellyfin.Plugin.ListenBrainz.Extensions;

/// <summary>
/// Extensions for <see cref="User"/> type.
/// </summary>
public static class UserExtensions
{
    /// <summary>
    /// Get ListenBrainz config for this user.
    /// </summary>
    /// <param name="user">Jellyfin user.</param>
    /// <returns>ListenBrainz config. Null if not available.</returns>
    public static UserConfig GetListenBrainzConfig(this User user)
    {
        var userConfig = Plugin
            .GetConfiguration()
            .UserConfigs
            .FirstOrDefault(u => u.JellyfinUserId == user.Id);

        if (userConfig is null)
        {
            throw new PluginException("User configuration is not available (unconfigured user?)");
        }

        return userConfig;
    }
}
