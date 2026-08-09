using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Playlist = Jellyfin.Plugin.ListenBrainz.Api.Models.Playlist;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks;

public class SyncPlaylistsTaskTests
{
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly Mock<IUserManager> _userManagerMock;
    private readonly Mock<IPlaylistManager> _playlistManagerMock;
    private readonly Mock<IListenBrainzService> _listenBrainzServiceMock;
    private readonly Mock<IMetadataProviderService> _metadataProviderServiceMock;
    private readonly Mock<IPluginConfigService> _configServiceMock;
    private readonly SyncPlaylistsTask _task;
    private readonly Mock<IProgress<double>> _progressMock;

    public SyncPlaylistsTaskTests()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        loggerFactoryMock
            .Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        _libraryManagerMock = new Mock<ILibraryManager>();
        _userManagerMock = new Mock<IUserManager>();
        _playlistManagerMock = new Mock<IPlaylistManager>();
        _listenBrainzServiceMock = new Mock<IListenBrainzService>();
        _metadataProviderServiceMock = new Mock<IMetadataProviderService>();
        _configServiceMock = new Mock<IPluginConfigService>();

        _task = new SyncPlaylistsTask(
            loggerFactoryMock.Object,
            _libraryManagerMock.Object,
            _userManagerMock.Object,
            _playlistManagerMock.Object,
            _listenBrainzServiceMock.Object,
            _metadataProviderServiceMock.Object,
            _configServiceMock.Object);

        _progressMock = new Mock<IProgress<double>>();
    }

    private static User GetUser() => new("foobar", "auth-provider-id", "pw-reset-provider-id");

    private static UserConfig GetUserConfig(Guid userId) => new()
    {
        JellyfinUserId = userId,
        UserName = "foobar",
        IsPlaylistsSyncEnabled = true,
    };

    private static Playlist GetPlaylist(string id) => new() { Identifier = $"https://listenbrainz.org/playlist/{id}" };

    private void SetupCommonMocks(User user)
    {
        _configServiceMock
            .SetupGet(cs => cs.LibraryConfigs)
            .Returns([new LibraryConfig { Id = Guid.NewGuid(), IsAllowed = true }]);

        _userManagerMock
            .Setup(um => um.GetUserById(user.Id))
            .Returns(user);

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns((BaseItem?)null);

        _libraryManagerMock
            .Setup(lm => lm.GetItemList(It.IsAny<InternalItemsQuery>(), It.IsAny<List<BaseItem>>()))
            .Returns(new List<BaseItem>());

        _listenBrainzServiceMock
            .Setup(lb => lb.GetPlaylistAsync(It.IsAny<UserConfig>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not needed for this test"));
    }

    [Fact]
    public async Task ExecuteAsync_MergesOwnPlaylists_DedupingAgainstCreatedForPlaylists_WhenAllPlaylistsSyncEnabled()
    {
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id);
        SetupCommonMocks(user);

        _configServiceMock.SetupGet(cs => cs.UserConfigs).Returns([userConfig]);
        _configServiceMock.SetupGet(cs => cs.IsAllPlaylistsSyncEnabled).Returns(true);

        _listenBrainzServiceMock
            .Setup(lb => lb.GetCreatedForPlaylistsAsync(userConfig, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([GetPlaylist("shared-id")]);
        _listenBrainzServiceMock
            .Setup(lb => lb.GetUserPlaylistsAsync(userConfig, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([GetPlaylist("shared-id"), GetPlaylist("own-id")]);

        await _task.ExecuteAsync(_progressMock.Object, CancellationToken.None);

        _listenBrainzServiceMock.Verify(
            lb => lb.GetUserPlaylistsAsync(userConfig, It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _listenBrainzServiceMock.Verify(
            lb => lb.GetPlaylistAsync(userConfig, "shared-id", It.IsAny<CancellationToken>()),
            Times.Once);
        _listenBrainzServiceMock.Verify(
            lb => lb.GetPlaylistAsync(userConfig, "own-id", It.IsAny<CancellationToken>()),
            Times.Once);
        _listenBrainzServiceMock.Verify(
            lb => lb.GetPlaylistAsync(It.IsAny<UserConfig>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_DoesNotFetchOwnPlaylists_WhenAllPlaylistsSyncDisabled()
    {
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id);
        SetupCommonMocks(user);

        _configServiceMock.SetupGet(cs => cs.UserConfigs).Returns([userConfig]);
        _configServiceMock.SetupGet(cs => cs.IsAllPlaylistsSyncEnabled).Returns(false);

        _listenBrainzServiceMock
            .Setup(lb => lb.GetCreatedForPlaylistsAsync(userConfig, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([GetPlaylist("shared-id")]);

        await _task.ExecuteAsync(_progressMock.Object, CancellationToken.None);

        _listenBrainzServiceMock.Verify(
            lb => lb.GetUserPlaylistsAsync(It.IsAny<UserConfig>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
