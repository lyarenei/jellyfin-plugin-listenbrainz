using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.Listenbrainz.Configuration;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Listenbrainz
{
    /// <summary>
    /// Main plugin definition.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer) => Instance = this;

        /// <inheritdoc />
        public override string Name => "ListenBrainz";

        /// <inheritdoc />
        public override Guid Id => Guid.Parse("59B20823-AAFE-454C-A393-17427F518631");

        /// <summary>
        /// Gets plugin instance.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace),
                    EnableInMainMenu = true,
                    MenuIcon = "music_note"
                }
            };
        }

        /// <summary>
        /// Convenience method for getting plugin configuration.
        /// </summary>
        /// <returns>Plugin configuration.</returns>
        /// <exception cref="PluginInstanceException">Plugin instance is not available.</exception>
        public static PluginConfiguration GetConfiguration()
        {
            var config = Instance?.Configuration;
            if (config != null) return config;
            throw new PluginInstanceException("Plugin instance is NULL");
        }

        /// <summary>
        /// Convenience method for getting plugin data path.
        /// </summary>
        /// <returns>Plugin data path.</returns>
        /// <exception cref="PluginInstanceException">Plugin instance is not available.</exception>
        public static string GetDataPath()
        {
            var path = Plugin.Instance?.DataFolderPath;
            if (path != null) return path;
            throw new PluginInstanceException("Plugin instance is NULL");
        }
    }
}
