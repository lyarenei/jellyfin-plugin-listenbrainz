using System;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public class FavoriteSyncServiceTests
{
    private readonly Mock<IListenBrainzClient> _listenBrainzClientMock;
    private readonly Mock<IMusicBrainzClient> _musicBrainzClientMock;
    private readonly Mock<IPluginConfigService> _pluginConfigServiceMock;
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly Mock<IUserManager> _userManagerMock;
    private readonly Mock<IUserDataManager> _userDataManagerMock;
    private readonly IFavoriteSyncService _service;

    private static readonly Audio _item = new()
    {
        Id = Guid.NewGuid(),
        ProviderIds = { ["MusicBrainzRecording"] = "fake-mbid" }
    };

    private static readonly AudioItemMetadata _metadata = new() { RecordingMbid = "new-fake-mbid" };
    private static readonly User _jellyfinUser = new("newuser", "0", "0");

    private static readonly UserConfig _userConfig = new()
    {
        ApiToken = "fake-token",
        JellyfinUserId = _jellyfinUser.Id,
    };

    private static readonly UserItemData _itemData = new()
    {
        IsFavorite = true,
        Key = "fake-key",
    };

    public FavoriteSyncServiceTests()
    {
        _listenBrainzClientMock = new Mock<IListenBrainzClient>();
        _musicBrainzClientMock = new Mock<IMusicBrainzClient>();
        _pluginConfigServiceMock = new Mock<IPluginConfigService>();
        _libraryManagerMock = new Mock<ILibraryManager>();
        _userManagerMock = new Mock<IUserManager>();
        _userDataManagerMock = new Mock<IUserDataManager>();

        _service = new DefaultFavoriteSyncService(
            new NullLogger<DefaultFavoriteSyncService>(),
            _listenBrainzClientMock.Object,
            _musicBrainzClientMock.Object,
            _pluginConfigServiceMock.Object,
            _libraryManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object);
    }

    [Fact]
    public void SyncToListenBrainz_IsDisabled()
    {
        _service.Disable();
        _service.SyncToListenBrainz(_item.Id, _jellyfinUser.Id);
        _libraryManagerMock.Verify(m => m.GetItemById(It.IsAny<Guid>()), Times.Never);
        _listenBrainzClientMock.Verify(m => m.SendFeedback(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void SyncToListenBrainz_InvalidItemId()
    {
        _libraryManagerMock
            .Setup(m => m.GetItemById(It.IsAny<Guid>()))
            .Returns((BaseItem?)null);

        var invalidItemId = Guid.NewGuid();
        var jellyfinUserId = Guid.NewGuid();

        _service.SyncToListenBrainz(invalidItemId, jellyfinUserId);

        _libraryManagerMock.Verify(m => m.GetItemById(invalidItemId), Times.Once);
        _pluginConfigServiceMock.Verify(m => m.GetUserConfig(It.IsAny<Guid>()), Times.Never);
        _listenBrainzClientMock.Verify(m => m.SendFeedback(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void SyncToListenBrainz_NoUserConfig()
    {
        _libraryManagerMock
            .Setup(m => m.GetItemById(_item.Id))
            .Returns(_item);

        _pluginConfigServiceMock
            .Setup(m => m.GetUserConfig(_userConfig.JellyfinUserId))
            .Returns((UserConfig?)null);

        _service.SyncToListenBrainz(_item.Id, _userConfig.JellyfinUserId);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _pluginConfigServiceMock.Verify(m => m.GetUserConfig(_userConfig.JellyfinUserId), Times.Once);
        _userManagerMock.Verify(m => m.GetUserById(_userConfig.JellyfinUserId), Times.Never);
        _listenBrainzClientMock.Verify(m => m.SendFeedback(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void SyncToListenBrainz_JellyfinUserNotFound()
    {
        _libraryManagerMock
            .Setup(m => m.GetItemById(_item.Id))
            .Returns(_item);

        _pluginConfigServiceMock
            .Setup(m => m.GetUserConfig(_userConfig.JellyfinUserId))
            .Returns(_userConfig);

        _userManagerMock
            .Setup(m => m.GetUserById(_userConfig.JellyfinUserId))
            .Returns((User?)null);

        _service.SyncToListenBrainz(_item.Id, _userConfig.JellyfinUserId);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _pluginConfigServiceMock.Verify(m => m.GetUserConfig(_userConfig.JellyfinUserId), Times.Once);
        _userManagerMock.Verify(m => m.GetUserById(_userConfig.JellyfinUserId), Times.Once);
        _userDataManagerMock.Verify(m => m.GetUserData(_jellyfinUser, _item), Times.Never);
        _listenBrainzClientMock.Verify(m => m.SendFeedback(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void SyncToListenBrainz_OK()
    {
        _libraryManagerMock
            .Setup(m => m.GetItemById(_item.Id))
            .Returns(_item);

        _pluginConfigServiceMock
            .Setup(m => m.GetUserConfig(_userConfig.JellyfinUserId))
            .Returns(_userConfig);

        _userManagerMock
            .Setup(m => m.GetUserById(_userConfig.JellyfinUserId))
            .Returns(_jellyfinUser);

        _userDataManagerMock
            .Setup(m => m.GetUserData(_jellyfinUser, _item))
            .Returns(_itemData);

        _service.SyncToListenBrainz(_item.Id, _jellyfinUser.Id);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _pluginConfigServiceMock.Verify(m => m.GetUserConfig(_userConfig.JellyfinUserId), Times.Once);
        _userManagerMock.Verify(m => m.GetUserById(_userConfig.JellyfinUserId), Times.Once);
        _userDataManagerMock.Verify(m => m.GetUserData(_jellyfinUser, _item), Times.Once);
        _musicBrainzClientMock.Verify(m => m.GetAudioItemMetadata(_item), Times.Never);
        _listenBrainzClientMock.Verify(m => m.SendFeedback(
                _userConfig,
                _itemData.IsFavorite,
                _item.ProviderIds["MusicBrainzRecording"],
                null),
            Times.Once);
    }

    [Fact]
    public void SyncToListenBrainz_RecordingMbidNotAvailable()
    {
        var noMbidItem = new Audio
        {
            Id = _item.Id,
            ProviderIds = { ["MusicBrainzRecording"] = string.Empty }
        };

        _libraryManagerMock
            .Setup(m => m.GetItemById(_item.Id))
            .Returns(noMbidItem);

        _pluginConfigServiceMock
            .Setup(m => m.GetUserConfig(_userConfig.JellyfinUserId))
            .Returns(_userConfig);


        _userManagerMock
            .Setup(m => m.GetUserById(_userConfig.JellyfinUserId))
            .Returns(_jellyfinUser);

        _userDataManagerMock
            .Setup(m => m.GetUserData(_jellyfinUser, _item))
            .Returns(_itemData);

        _pluginConfigServiceMock
            .Setup(m => m.IsMusicBrainzEnabled)
            .Returns(true);

        _musicBrainzClientMock
            .Setup(m => m.GetAudioItemMetadata(_item))
            .Returns(_metadata);

        _service.SyncToListenBrainz(_item.Id, _jellyfinUser.Id);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _musicBrainzClientMock.Verify(m => m.GetAudioItemMetadata(_item), Times.Once);
        _listenBrainzClientMock.Verify(m => m.SendFeedback(
                _userConfig,
                _itemData.IsFavorite,
                _metadata.RecordingMbid,
                null),
            Times.Once);
    }

    [Fact]
    public void SyncToListenBrainz_MsidFallback()
    {
        var noMbidItem = new Audio { Id = _item.Id };

        _libraryManagerMock
            .Setup(m => m.GetItemById(_item.Id))
            .Returns(noMbidItem);

        _pluginConfigServiceMock
            .Setup(m => m.GetUserConfig(_userConfig.JellyfinUserId))
            .Returns(_userConfig);

        _pluginConfigServiceMock
            .Setup(m => m.IsMusicBrainzEnabled)
            .Returns(false);

        _userManagerMock
            .Setup(m => m.GetUserById(_userConfig.JellyfinUserId))
            .Returns(_jellyfinUser);

        _userDataManagerMock
            .Setup(m => m.GetUserData(_jellyfinUser, _item))
            .Returns(_itemData);

        _listenBrainzClientMock
            .Setup(m => m.GetRecordingMsidByListenTs(_userConfig, It.IsAny<long>()))
            .Returns("fake-msid");

        _service.SyncToListenBrainz(_item.Id, _jellyfinUser.Id, 12345);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _musicBrainzClientMock.Verify(m => m.GetAudioItemMetadata(_item), Times.Never);
        _listenBrainzClientMock.Verify(m => m.GetRecordingMsidByListenTs(
                _userConfig,
                12345),
            Times.Once);
        _listenBrainzClientMock.Verify(m => m.SendFeedback(
                _userConfig,
                _itemData.IsFavorite,
                null,
                "fake-msid"),
            Times.Once);
    }
}
