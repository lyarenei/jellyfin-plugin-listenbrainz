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
            ListenbrainzBaseUrl = Resources.Listenbrainz.Api.BaseUrl;
        }

        /// <summary>
        /// Gets or sets Listenbrainz users.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public Collection<LbUser> LbUsers { get; set; }

        /// <summary>
        /// Listenbrainz API base URL.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public string? ListenbrainzBaseUrl { get; set; }
    }
}
