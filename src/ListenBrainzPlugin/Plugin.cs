using System.Reflection;
using ListenBrainzPlugin.Configuration;
using ListenBrainzPlugin.Exceptions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace ListenBrainzPlugin;

/// <summary>
/// ListenBrainz Plugin definition for Jellyfin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private static Plugin? _thisInstance;

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="paths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(IApplicationPaths paths, IXmlSerializer xmlSerializer) : base(paths, xmlSerializer)
    {
        _thisInstance = this;
    }

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

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configurationPage.html",
                EnableInMainMenu = true,
                MenuIcon = "music_note"
            }
        };
    }

    /// <summary>
    /// Convenience method for getting plugin configuration.
    /// </summary>
    /// <returns>Plugin configuration.</returns>
    /// <exception cref="ListenBrainzPluginException">Plugin instance is not available.</exception>
    public static PluginConfiguration GetConfiguration()
    {
        var config = _thisInstance?.Configuration;
        if (config is not null) return config;
        throw new ListenBrainzPluginException("Plugin instance is not available");
    }
}
