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
            GlobalConfig = new GlobalConfiguration();
            LbUsers = new Collection<LbUser>();
        }

        /// <summary>
        /// Gets MusicBrainz API URL.
        /// </summary>
        /// <returns>API URL.</returns>
        public string MusicBrainzUrl => GlobalConfig.MusicbrainzBaseUrl ?? MusicBrainz.Resources.Api.BaseUrl;

        /// <summary>
        /// Gets ListenBrainz API URL.
        /// </summary>
        /// <returns>API URL.</returns>
        public string ListenBrainzUrl => GlobalConfig.ListenbrainzBaseUrl ?? Resources.Listenbrainz.Api.BaseUrl;

        /// <summary>
        /// Gets or sets plugin global configuration.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public GlobalConfiguration GlobalConfig { get; set; }

        /// <summary>
        /// Gets or sets plugin users.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public Collection<LbUser> LbUsers { get; set; }
    }
}