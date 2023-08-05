using Jellyfin.Data.Entities;
using Jellyfin.Plugin.ListenBrainz.Configuration;

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
    public static UserConfig? GetListenBrainzConfig(this User user)
    {
        return Plugin.GetConfiguration().UserConfigs.FirstOrDefault(u => u.JellyfinUserId == user.Id);
    }
}
