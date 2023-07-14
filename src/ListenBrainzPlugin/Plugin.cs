using ListenBrainzPlugin.Configuration;
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
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="paths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(IApplicationPaths paths, IXmlSerializer xmlSerializer) : base(paths, xmlSerializer)
    {
    }

    /// <inheritdoc />
    public override string Name => "ListenBrainz";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("ec60dece-f76f-4996-bf2b-3c26d5ae4597");

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
}
