using System.IO;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Plugin.ListenBrainz.Tests;

public class MockPlugin : Plugin
{
    private const string ConfigFileName = "Jellyfin.Plugin.ListenBrainz.Tests.xml";

    public readonly Mock<IApplicationPaths> _pathsMock;
    public readonly Mock<IXmlSerializer> _xmlSerializerMock;

    public MockPlugin(
        Mock<IApplicationPaths> paths,
        Mock<IXmlSerializer> xmlSerializer) : base(
        paths.Object,
        xmlSerializer.Object,
        NullLoggerFactory.Instance)
    {
        _pathsMock = paths;
        _xmlSerializerMock = xmlSerializer;
    }

    public static MockPlugin Init(
        Mock<IApplicationPaths> pathsMock,
        Mock<IXmlSerializer> xmlSerializerMock,
        PluginConfiguration configuration)
    {
        // Necessary setup or plugin instance crashes
        var configDir = Directory.CreateTempSubdirectory("lb-mock-plugin").FullName;
        pathsMock.Setup(p => p.PluginConfigurationsPath).Returns(configDir);
        pathsMock.Setup(p => p.PluginsPath).Returns(configDir);

        xmlSerializerMock
            .Setup(x => x.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
            .Returns(configuration);

        // The plugin only loads the configuration when its file exists, the content can be anything
        File.WriteAllText(Path.Join(configDir, ConfigFileName), string.Empty);

        return new MockPlugin(pathsMock, xmlSerializerMock);
    }
}
