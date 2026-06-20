using System.Globalization;
using System.Reflection;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.ListenBrainz;

/// <summary>
/// ListenBrainz Plugin definition for Jellyfin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="paths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(IApplicationPaths paths, IXmlSerializer xmlSerializer) : base(paths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "ListenBrainz";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("59B20823-AAFE-454C-A393-17427F518631");

    /// <summary>
    /// Gets plugin version.
    /// </summary>
    public static new string Version
    {
        get => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0";
    }

    /// <summary>
    /// Gets full plugin name.
    /// </summary>
    public static string FullName => "ListenBrainz plugin for Jellyfin";

    /// <summary>
    /// Gets plugin source URL.
    /// </summary>
    public static string SourceUrl => "https://github.com/lyarenei/jellyfin-plugin-listenbrainz";

    /// <summary>
    /// Gets logger category.
    /// </summary>
    public static string LoggerCategory => "Jellyfin.Plugin.ListenBrainz";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Pages.Configuration.index.html",
                    GetType().Namespace),
            },
            new PluginPageInfo
            {
                Name = $"{Name}.js",
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Pages.Configuration.index.js",
                    GetType().Namespace),
            },
            new PluginPageInfo
            {
                Name = $"{Name}.styles.css",
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Pages.Configuration.styles.css",
                    GetType().Namespace),
            },
        ];
    }

    /// <summary>
    /// Gets plugin data path.
    /// </summary>
    /// <returns>Path to the plugin data folder.</returns>
    /// <exception cref="PluginException">Plugin instance is not available.</exception>
    public static string GetDataPath()
    {
        var instance = Instance ?? throw new PluginException("Plugin instance is not available");

        // DataFolderPath is invalid (https://github.com/jellyfin/jellyfin/issues/10091)
        // var path = instance.DataFolderPath;
        var pluginDirName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}", instance.Name, Version);
        return Path.Join(instance.ApplicationPaths.PluginsPath, pluginDirName);
    }

    /// <summary>
    /// Gets plugin configuration directory path.
    /// </summary>
    /// <returns>Path to config directory.</returns>
    /// <exception cref="PluginException">Plugin instance or path is not available.</exception>
    public static string GetConfigDirPath()
    {
        var instance = Instance ?? throw new PluginException("Plugin instance is not available");
        var dirName = Path.GetDirectoryName(instance.ConfigurationFilePath);
        if (dirName is null)
        {
            throw new PluginException("Could not get a config directory name");
        }

        return dirName;
    }
}
