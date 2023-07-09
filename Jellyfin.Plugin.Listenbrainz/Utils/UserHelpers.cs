using System;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models;

namespace Jellyfin.Plugin.Listenbrainz.Utils;

/// <summary>
/// User helpers.
/// </summary>
public static class UserHelpers
{
    /// <summary>
    /// Get ListenBrainz user by Jellyfin <see cref="User"/>.
    /// </summary>
    /// <param name="user">Jellyfin user.</param>
    /// <returns>ListenBrainz user. Null if not found.</returns>
    public static LbUser? GetListenBrainzUser(User? user)
    {
        return user != null ? GetUser(user.Id) : null;
    }

    private static LbUser? GetUser(Guid userId)
    {
        var config = Plugin.GetConfiguration();
        return config.LbUsers.FirstOrDefault(u => u.MediaBrowserUserId.Equals(userId));
    }
}
