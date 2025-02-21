using System.Net.Http;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using System;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks;

public class ResubmitListensTaskTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<IHttpClientFactory> _clientFactoryMock;
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly Mock<IUserManager> _userManagerMock;
    private readonly Mock<IListensCacheManager> _listensCacheManagerMock;
    private readonly Mock<IListenBrainzClient> _listenBrainzClientMock;
    private readonly Mock<IMusicBrainzClient> _musicBrainzClientMock;
    private readonly Plugin _plugin;
    private readonly ResubmitListensTask _task;

    public ResubmitListensTaskTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _clientFactoryMock = new Mock<IHttpClientFactory>();
        _libraryManagerMock = new Mock<ILibraryManager>();
        _userManagerMock = new Mock<IUserManager>();
        _listensCacheManagerMock = new Mock<IListensCacheManager>();
        _listenBrainzClientMock = new Mock<IListenBrainzClient>();
        _musicBrainzClientMock = new Mock<IMusicBrainzClient>();
        _task = new ResubmitListensTask(
            _loggerFactoryMock.Object,
            _clientFactoryMock.Object,
            _userManagerMock.Object,
            _libraryManagerMock.Object,
            _listensCacheManagerMock.Object,
            _listenBrainzClientMock.Object,
            _musicBrainzClientMock.Object);

        _plugin = MockPlugin.Init(
            new Mock<IApplicationPaths>(),
            new Mock<IXmlSerializer>(),
            new Mock<ISessionManager>(),
            _loggerFactoryMock,
            _clientFactoryMock,
            new Mock<IUserDataManager>(),
            _libraryManagerMock,
            _userManagerMock,
            new Mock<IHostedService>(),
            new PluginConfiguration());
    }

    [Fact]
    public void GetInterval_ReturnValidInterval()
    {
        var interval = ResubmitListensTask.GetInterval();
        // Should be between 24h and 24h50m
        Assert.True(interval > TimeSpan.TicksPerDay);
        Assert.True(interval <= TimeSpan.TicksPerDay + (50 * TimeSpan.TicksPerMinute));
    }

    [Fact]
    public void GetInterval_ConsecutiveCallsReturnDifferentValues()
    {
        var interval1 = ResubmitListensTask.GetInterval();
        var interval2 = ResubmitListensTask.GetInterval();
        Assert.NotEqual(interval1, interval2);
    }
}
