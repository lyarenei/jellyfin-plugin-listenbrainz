using System;
using System.Linq;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Models;

namespace Jellyfin.Plugin.Listenbrainz.Utils
{
    public static class UserHelpers
    {
        public static LbUser GetUser(Guid userId) => Plugin.Instance.Configuration.LbUsers.FirstOrDefault(u => u.MediaBrowserUserId.Equals(userId));

        public static LbUser GetUser(User user)
        {
            if (user == null || Plugin.Instance.Configuration.LbUsers == null)
                return null;

            return GetUser(user.Id);
        }
    }
}
