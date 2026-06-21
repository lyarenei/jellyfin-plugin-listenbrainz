using Jellyfin.Plugin.ListenBrainz.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Moq;

namespace Jellyfin.Plugin.ListenBrainz.Tests;

public class MockPlugin : Plugin
{
    public readonly Mock<IApplicationPaths> _pathsMock;
    public readonly Mock<IXmlSerializer> _xmlSerializerMock;

    public MockPlugin(
        Mock<IApplicationPaths> paths,
        Mock<IXmlSerializer> xmlSerializer) : base(
        paths.Object,
        xmlSerializer.Object)
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
        pathsMock.Setup(p => p.PluginConfigurationsPath).Returns("some-path");
        pathsMock.Setup(p => p.PluginsPath).Returns("some-path");

        xmlSerializerMock
            .Setup(x => x.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
            .Returns(configuration);

        return new MockPlugin(pathsMock, xmlSerializerMock);
    }
}
