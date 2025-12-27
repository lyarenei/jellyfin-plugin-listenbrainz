using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
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
    private readonly Mock<IListenBrainzService> _listenBrainzServiceMock;
    private readonly Mock<IMetadataProviderService> _metadataProviderMock;
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
        _listenBrainzServiceMock = new Mock<IListenBrainzService>();
        _metadataProviderMock = new Mock<IMetadataProviderService>();
        _pluginConfigServiceMock = new Mock<IPluginConfigService>();
        _libraryManagerMock = new Mock<ILibraryManager>();
        _userManagerMock = new Mock<IUserManager>();
        _userDataManagerMock = new Mock<IUserDataManager>();

        _service = new DefaultFavoriteSyncService(
            new NullLogger<DefaultFavoriteSyncService>(),
            _listenBrainzServiceMock.Object,
            _metadataProviderMock.Object,
            _pluginConfigServiceMock.Object,
            _libraryManagerMock.Object,
            _userManagerMock.Object,
            _userDataManagerMock.Object);
    }

    [Fact]
    public async Task SyncToListenBrainz_IsDisabled()
    {
        _service.Disable();
        await _service.SyncToListenBrainzAsync(_item.Id, _jellyfinUser.Id, cancellationToken: CancellationToken.None);
        _libraryManagerMock.Verify(m => m.GetItemById(It.IsAny<Guid>()), Times.Never);
        _listenBrainzServiceMock.Verify(m => m.SendFeedbackAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task SyncToListenBrainz_InvalidItemId()
    {
        _libraryManagerMock
            .Setup(m => m.GetItemById(It.IsAny<Guid>()))
            .Returns((BaseItem?)null);

        var invalidItemId = Guid.NewGuid();
        var jellyfinUserId = Guid.NewGuid();

        await _service.SyncToListenBrainzAsync(
            invalidItemId,
            jellyfinUserId,
            cancellationToken: CancellationToken.None);

        _libraryManagerMock.Verify(m => m.GetItemById(invalidItemId), Times.Once);
        _pluginConfigServiceMock.Verify(m => m.GetUserConfig(It.IsAny<Guid>()), Times.Never);
        _listenBrainzServiceMock.Verify(m => m.SendFeedbackAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task SyncToListenBrainz_NoUserConfig()
    {
        _libraryManagerMock
            .Setup(m => m.GetItemById(_item.Id))
            .Returns(_item);

        _pluginConfigServiceMock
            .Setup(m => m.GetUserConfig(_userConfig.JellyfinUserId))
            .Returns((UserConfig?)null);

        await _service.SyncToListenBrainzAsync(
            _item.Id,
            _userConfig.JellyfinUserId,
            cancellationToken: CancellationToken.None);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _pluginConfigServiceMock.Verify(m => m.GetUserConfig(_userConfig.JellyfinUserId), Times.Once);
        _userManagerMock.Verify(m => m.GetUserById(_userConfig.JellyfinUserId), Times.Never);
        _listenBrainzServiceMock.Verify(m => m.SendFeedbackAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task SyncToListenBrainz_JellyfinUserNotFound()
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

        await _service.SyncToListenBrainzAsync(
            _item.Id,
            _userConfig.JellyfinUserId,
            cancellationToken: CancellationToken.None);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _pluginConfigServiceMock.Verify(m => m.GetUserConfig(_userConfig.JellyfinUserId), Times.Once);
        _userManagerMock.Verify(m => m.GetUserById(_userConfig.JellyfinUserId), Times.Once);
        _userDataManagerMock.Verify(m => m.GetUserData(_jellyfinUser, _item), Times.Never);
        _listenBrainzServiceMock.Verify(m => m.SendFeedbackAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public async Task SyncToListenBrainz_OK()
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

        await _service.SyncToListenBrainzAsync(_item.Id, _jellyfinUser.Id, cancellationToken: CancellationToken.None);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _pluginConfigServiceMock.Verify(m => m.GetUserConfig(_userConfig.JellyfinUserId), Times.Once);
        _userManagerMock.Verify(m => m.GetUserById(_userConfig.JellyfinUserId), Times.Once);
        _userDataManagerMock.Verify(m => m.GetUserData(_jellyfinUser, _item), Times.Once);
        _metadataProviderMock.Verify(m => m.GetAudioItemMetadataAsync(_item, CancellationToken.None), Times.Never);
        _listenBrainzServiceMock.Verify(m => m.SendFeedbackAsync(
                _userConfig,
                _itemData.IsFavorite,
                _item.ProviderIds["MusicBrainzRecording"],
                null,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task SyncToListenBrainz_RecordingMbidNotAvailable()
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

        _metadataProviderMock
            .Setup(m => m.GetAudioItemMetadataAsync(_item, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_metadata)!);

        await _service.SyncToListenBrainzAsync(_item.Id, _jellyfinUser.Id, cancellationToken: CancellationToken.None);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _metadataProviderMock.Verify(m => m.GetAudioItemMetadataAsync(_item, CancellationToken.None), Times.Once);
        _listenBrainzServiceMock.Verify(m => m.SendFeedbackAsync(
                _userConfig,
                _itemData.IsFavorite,
                _metadata.RecordingMbid,
                null,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task SyncToListenBrainz_MsidFallback()
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

        _listenBrainzServiceMock
            .Setup(m => m.GetRecordingMsidByListenTsAsync(_userConfig, It.IsAny<long>(), CancellationToken.None))
            .Returns(Task.FromResult("fake-msid"));

        await _service.SyncToListenBrainzAsync(_item.Id, _jellyfinUser.Id, 12345, CancellationToken.None);

        _libraryManagerMock.Verify(m => m.GetItemById(_item.Id), Times.Once);
        _metadataProviderMock.Verify(m => m.GetAudioItemMetadataAsync(_item, CancellationToken.None), Times.Never);
        _listenBrainzServiceMock.Verify(m => m.GetRecordingMsidByListenTsAsync(
                _userConfig,
                12345,
                CancellationToken.None),
            Times.Once);
        _listenBrainzServiceMock.Verify(m => m.SendFeedbackAsync(
                _userConfig,
                _itemData.IsFavorite,
                null,
                "fake-msid",
                CancellationToken.None),
            Times.Once);
    }
}
