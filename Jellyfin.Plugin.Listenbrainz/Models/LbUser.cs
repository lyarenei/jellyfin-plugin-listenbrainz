using System;

namespace Jellyfin.Plugin.Listenbrainz.Models
{
    public class LbUser
    {
        public string Name { get; set; }
        public string Token { get; set; }
        public Guid MediaBrowserUserId { get; set; }
        public LbUserOptions Options { get; set; }
    }

    public class LbUserOptions
    {
        public bool ListenSubmitEnabled { get; set; }
    }
}
