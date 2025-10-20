using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks;

public class LovedTracksSyncTaskTests
{
    private readonly Mock<IHttpClientFactory> _clientFactoryMock;
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly Mock<IUserManager> _userManagerMock;
    private readonly Mock<IUserDataManager> _userDataManagerMock;
    private readonly Mock<IListenBrainzClient> _listenBrainzClientMock;
    private readonly Mock<IMusicBrainzClient> _musicBrainzClientMock;
    private readonly Mock<IPluginConfigService> _pluginConfigServiceMock;
    private readonly Mock<IFavoriteSyncService> _favoriteSyncServiceMock;
    private readonly LovedTracksSyncTask _task;
    private readonly Mock<IProgress<double>> _progressMock;

    public LovedTracksSyncTaskTests()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(new NullLogger<ResubmitListensTask>());

        _clientFactoryMock = new Mock<IHttpClientFactory>();
        _libraryManagerMock = new Mock<ILibraryManager>();
        _userManagerMock = new Mock<IUserManager>();
        _userDataManagerMock = new Mock<IUserDataManager>();
        _listenBrainzClientMock = new Mock<IListenBrainzClient>();
        _musicBrainzClientMock = new Mock<IMusicBrainzClient>();
        _pluginConfigServiceMock = new Mock<IPluginConfigService>();
        _favoriteSyncServiceMock = new Mock<IFavoriteSyncService>();

        _task = new LovedTracksSyncTask(
            loggerFactoryMock.Object,
            _clientFactoryMock.Object,
            _libraryManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object,
            _listenBrainzClientMock.Object,
            _musicBrainzClientMock.Object,
            _pluginConfigServiceMock.Object,
            _favoriteSyncServiceMock.Object);

        _progressMock = new Mock<IProgress<double>>();
    }

    private static User GetUser() => new("foobar", "auth-provider-id", "pw-reset-provider-id");

    private static UserConfig GetUserConfig(Guid userId) => new()
    {
        JellyfinUserId = userId,
        UserName = "foobar",
        IsListenSubmitEnabled = true,
        ApiToken = "some-token",
        PlaintextApiToken = "some-token",
    };

    [Fact]
    public async Task ExecuteAsync_ExitEarlyNoUsers()
    {
        _pluginConfigServiceMock
            .SetupGet(m => m.IsMusicBrainzEnabled)
            .Returns(true);

        _pluginConfigServiceMock
            .SetupGet(m => m.UserConfigs)
            .Returns([]);

        await _task.ExecuteAsync(_progressMock.Object, CancellationToken.None);

        _pluginConfigServiceMock.VerifyGet(pcm => pcm.UserConfigs, Times.Once);
        _progressMock.Verify(pm => pm.Report(100), Times.Once);
        _listenBrainzClientMock.Verify(
            lbc => lbc.GetLovedTracksAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
