using Jellyfin.Plugin.ListenBrainz.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.ListenBrainz.Tests.TestKit;

/// <summary>
/// A plugin instance backed by a throwaway configuration directory.
/// </summary>
public sealed class MockPlugin : IDisposable
{
    private const string ConfigFileName = "Jellyfin.Plugin.ListenBrainz.Tests.xml";

    private readonly TempDir _configDir = new("lb-mock-plugin");

    /// <summary>
    /// Initializes a new instance of the <see cref="MockPlugin"/> class.
    /// </summary>
    /// <param name="configuration">Configuration the plugin should load, defaults to an empty one.</param>
    public MockPlugin(PluginConfiguration? configuration = null)
    {
        var paths = new Mock<IApplicationPaths>();
        paths.Setup(p => p.PluginConfigurationsPath).Returns(_configDir.Path);
        paths.Setup(p => p.PluginsPath).Returns(_configDir.Path);

        var xmlSerializer = new Mock<IXmlSerializer>();
        xmlSerializer
            .Setup(x => x.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
            .Returns(configuration ?? new PluginConfiguration());

        // The plugin only loads the configuration when its file exists, the content can be anything.
        File.WriteAllText(_configDir.File(ConfigFileName), string.Empty);

        Plugin = new Plugin(paths.Object, xmlSerializer.Object, NullLoggerFactory.Instance);
    }

    /// <summary>
    /// Gets the plugin instance.
    /// </summary>
    public Plugin Plugin { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        Plugin.Instance = null;
        _configDir.Dispose();
    }
}
