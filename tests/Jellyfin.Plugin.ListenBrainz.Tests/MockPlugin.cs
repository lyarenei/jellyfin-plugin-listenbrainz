using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.ListenBrainz.Tests;

public class MockPlugin : Plugin
{
    public readonly Mock<IApplicationPaths> _pathsMock;
    public readonly Mock<IXmlSerializer> _xmlSerializerMock;
    public readonly Mock<ISessionManager> _sessionManagerMock;
    public readonly Mock<ILoggerFactory> _loggerFactoryMock;
    public readonly Mock<IHttpClientFactory> _clientFactoryMock;
    public readonly Mock<IUserDataManager> _userDataManagerMock;
    public readonly Mock<ILibraryManager> _libraryManagerMock;
    public readonly Mock<IUserManager> _userManagerMock;

    public MockPlugin(
        Mock<IApplicationPaths> paths,
        Mock<IXmlSerializer> xmlSerializer,
        Mock<ISessionManager> sessionManager,
        Mock<ILoggerFactory> loggerFactory,
        Mock<IHttpClientFactory> clientFactory,
        Mock<IUserDataManager> userDataManager,
        Mock<ILibraryManager> libraryManager,
        Mock<IUserManager> userManager,
        Mock<IHostedService> pluginService) : base(
        paths.Object,
        xmlSerializer.Object,
        sessionManager.Object,
        loggerFactory.Object,
        clientFactory.Object,
        userDataManager.Object,
        libraryManager.Object,
        userManager.Object)
    {
        _pathsMock = paths;
        _xmlSerializerMock = xmlSerializer;
        _sessionManagerMock = sessionManager;
        _loggerFactoryMock = loggerFactory;
        _clientFactoryMock = clientFactory;
        _userDataManagerMock = userDataManager;
        _libraryManagerMock = libraryManager;
        _userManagerMock = userManager;
    }

    public static MockPlugin Init(
        Mock<IApplicationPaths> pathsMock,
        Mock<IXmlSerializer> xmlSerializerMock,
        Mock<ISessionManager> sessionManagerMock,
        Mock<ILoggerFactory> loggerFactoryMock,
        Mock<IHttpClientFactory> clientFactoryMock,
        Mock<IUserDataManager> userDataManagerMock,
        Mock<ILibraryManager> libraryManagerMock,
        Mock<IUserManager> userManagerMock,
        Mock<IHostedService> pluginServiceMock,
        PluginConfiguration configuration)
    {
        // Necessary setup or plugin instance crashes
        pathsMock.Setup(p => p.PluginConfigurationsPath).Returns("some-path");
        pathsMock.Setup(p => p.PluginsPath).Returns("some-path");

        xmlSerializerMock
            .Setup(x => x.DeserializeFromFile(typeof(PluginConfiguration), It.IsAny<string>()))
            .Returns(configuration);

        pluginServiceMock
            .Setup(s => s.StartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return new MockPlugin(
            pathsMock,
            xmlSerializerMock,
            sessionManagerMock,
            loggerFactoryMock,
            clientFactoryMock,
            userDataManagerMock,
            libraryManagerMock,
            userManagerMock,
            pluginServiceMock);
    }
}
