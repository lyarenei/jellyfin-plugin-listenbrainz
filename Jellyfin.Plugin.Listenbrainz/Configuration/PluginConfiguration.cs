using System;
using Jellyfin.Plugin.Listenbrainz.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Listenbrainz.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        public LbUser[] LbUsers { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            LbUsers = Array.Empty<LbUser>();
        }
    }
}
