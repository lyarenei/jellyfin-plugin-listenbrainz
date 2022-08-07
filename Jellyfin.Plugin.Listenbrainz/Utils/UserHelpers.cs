using System;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models;

namespace Jellyfin.Plugin.Listenbrainz.Utils
{
    /// <summary>
    /// User helpers.
    /// </summary>
    public static class UserHelpers
    {
        /// <summary>
        /// Get Listenbrainz user by jellyfin GUID.
        /// </summary>
        /// <param name="userId">Jellyfin GUID.</param>
        /// <returns>Listenbrainz user.</returns>
        public static LbUser? GetUser(Guid userId)
        {
            try
            {
                return Plugin.Instance?.Configuration.LbUsers.First(u => u.MediaBrowserUserId.Equals(userId));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Get Listenbrainz user by jellyfin <see cref="User"/>.
        /// </summary>
        /// <param name="user">Jellyfin user.</param>
        /// <returns>Listenbrainz user. Null if not found.</returns>
        public static LbUser? GetUser(User? user)
        {
            return user != null ? GetUser(user.Id) : null;
        }
    }
}
