using System;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.ResubmitListens;

public class TestBase
{
    protected readonly Mock<ILibraryManager> _libraryManagerMock;
    protected readonly Mock<IListensCachingService> _listensCachingServiceMock;
    protected readonly Mock<IListenBrainzService> _listenBrainzServiceMock;
    protected readonly Mock<IMetadataProviderService> _metadataProviderServiceMock;
    protected readonly Mock<IPluginConfigService> _pluginConfigServiceMock;
    protected readonly Mock<IValidationService> _validationServiceMock;
    protected readonly ResubmitListensTask _task;

    public TestBase()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(new NullLogger<ResubmitListensTask>());

        _libraryManagerMock = new Mock<ILibraryManager>();
        _listensCachingServiceMock = new Mock<IListensCachingService>();
        _listenBrainzServiceMock = new Mock<IListenBrainzService>();
        _metadataProviderServiceMock = new Mock<IMetadataProviderService>();
        _pluginConfigServiceMock = new Mock<IPluginConfigService>();
        _validationServiceMock = new Mock<IValidationService>();

        _task = new ResubmitListensTask(
            loggerFactoryMock.Object,
            _libraryManagerMock.Object,
            _listenBrainzServiceMock.Object,
            _metadataProviderServiceMock.Object,
            _pluginConfigServiceMock.Object,
            _listensCachingServiceMock.Object,
            _validationServiceMock.Object);
    }

    internal static StoredListen[] GetStoredListens() =>
    [
        new()
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345567890,
        },
        new()
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345567891,
        },
    ];
}
