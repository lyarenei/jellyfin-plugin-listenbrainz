using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Jellyfin.Plugin.Listenbrainz.Models;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Listenbrainz.Configuration
{
    /// <summary>
    /// Class PluginConfiguration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            LbUsers = new Collection<LbUser>();
        }

        /// <summary>
        /// Gets or sets listenbrainz users.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public Collection<LbUser> LbUsers { get; set; }
    }
}
