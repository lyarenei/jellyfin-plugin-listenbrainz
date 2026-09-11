using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using Jellyfin.Plugin.ListenBrainz.Tests.TestKit;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public class FavoriteSyncServiceTests : ServiceTest<DefaultFavoriteSyncService>
{
    private const string ItemMbid = "fake-mbid";

    private readonly User _user = TestData.User("newuser");
    private readonly UserConfig _userConfig;
    private readonly UserItemData _itemData = new() { IsFavorite = true, Key = "fake-key" };
    private readonly AudioItemMetadata _metadata = new() { RecordingMbid = "new-fake-mbid" };

    public FavoriteSyncServiceTests()
    {
        _userConfig = TestData.UserConfig(_user.Id);
    }

    private Mock<ILibraryManager> LibraryManager => MockOf<ILibraryManager>();

    private Mock<IListenBrainzService> ListenBrainz => MockOf<IListenBrainzService>();

    private Mock<IMetadataProviderService> MetadataProvider => MockOf<IMetadataProviderService>();

    private Mock<IPluginConfigService> PluginConfig => MockOf<IPluginConfigService>();

    private Mock<IUserManager> UserManager => MockOf<IUserManager>();

    private Mock<IUserDataManager> UserDataManager => MockOf<IUserDataManager>();

    [Fact]
    public async Task SyncToListenBrainz_IsDisabled()
    {
        var item = GivenFavoritedItem();

        TestedService.Disable();
        await Sync(item);

        LibraryManager.Verify(m => m.GetItemById(It.IsAny<Guid>()), Times.Never);
        VerifyNoFeedbackSent();
    }

    [Fact]
    public async Task SyncToListenBrainz_InvalidItemId()
    {
        var unknownItemId = Guid.NewGuid();
        LibraryManager.Setup(m => m.GetItemById(It.IsAny<Guid>())).Returns((BaseItem?)null);

        await TestedService.SyncToListenBrainzAsync(unknownItemId, _user.Id, null, CancellationToken.None);

        LibraryManager.Verify(m => m.GetItemById(unknownItemId), Times.Once);
        PluginConfig.Verify(m => m.GetUserConfig(It.IsAny<Guid>()), Times.Never);
        VerifyNoFeedbackSent();
    }

    [Fact]
    public async Task SyncToListenBrainz_NoUserConfig()
    {
        var item = GivenFavoritedItem();
        PluginConfig.Setup(m => m.GetUserConfig(_user.Id)).Returns((UserConfig?)null);

        await Sync(item);

        PluginConfig.Verify(m => m.GetUserConfig(_user.Id), Times.Once);
        UserManager.Verify(m => m.GetUserById(_user.Id), Times.Never);
        VerifyNoFeedbackSent();
    }

    [Fact]
    public async Task SyncToListenBrainz_JellyfinUserNotFound()
    {
        var item = GivenFavoritedItem();
        UserManager.Setup(m => m.GetUserById(_user.Id)).Returns((User?)null);

        await Sync(item);

        UserManager.Verify(m => m.GetUserById(_user.Id), Times.Once);
        UserDataManager.Verify(m => m.GetUserData(_user, item), Times.Never);
        VerifyNoFeedbackSent();
    }

    [Fact]
    public async Task SyncToListenBrainz_OK()
    {
        var item = GivenFavoritedItem();

        await Sync(item);

        UserDataManager.Verify(m => m.GetUserData(_user, item), Times.Once);
        MetadataProvider.Verify(m => m.GetAudioItemMetadataAsync(item, CancellationToken.None), Times.Never);
        VerifyFeedbackSent(mbid: ItemMbid, msid: null);
    }

    [Fact]
    public async Task SyncToListenBrainz_RecordingMbidNotAvailable()
    {
        var item = GivenFavoritedItem(recordingMbid: string.Empty);
        PluginConfig.Setup(m => m.IsMusicBrainzEnabled).Returns(true);
        MetadataProvider
            .Setup(m => m.GetAudioItemMetadataAsync(item, CancellationToken.None))
            .ReturnsAsync(_metadata);

        await Sync(item);

        MetadataProvider.Verify(m => m.GetAudioItemMetadataAsync(item, CancellationToken.None), Times.Once);
        VerifyFeedbackSent(mbid: _metadata.RecordingMbid, msid: null);
    }

    [Fact]
    public async Task SyncToListenBrainz_MsidFallback()
    {
        var item = GivenFavoritedItem(recordingMbid: null);
        PluginConfig.Setup(m => m.IsMusicBrainzEnabled).Returns(false);
        ListenBrainz
            .Setup(m => m.GetRecordingMsidByListenTsAsync(_userConfig, It.IsAny<long>(), CancellationToken.None))
            .ReturnsAsync("fake-msid");

        await Sync(item, listenTs: 12345);

        MetadataProvider.Verify(m => m.GetAudioItemMetadataAsync(item, CancellationToken.None), Times.Never);
        ListenBrainz.Verify(
            m => m.GetRecordingMsidByListenTsAsync(_userConfig, 12345, CancellationToken.None),
            Times.Once);
        VerifyFeedbackSent(mbid: null, msid: "fake-msid");
    }

    /// <summary>
    /// Arranges the whole path from an item ID to its favorite state. Tests which exercise a break
    /// in that chain re-stub only the step they are about.
    /// </summary>
    private Audio GivenFavoritedItem(string? recordingMbid = ItemMbid)
    {
        var item = TestData.Audio(recordingMbid: recordingMbid);
        LibraryManager.Setup(m => m.GetItemById(item.Id)).Returns(item);
        PluginConfig.Setup(m => m.GetUserConfig(_user.Id)).Returns(_userConfig);
        UserManager.Setup(m => m.GetUserById(_user.Id)).Returns(_user);
        UserDataManager.Setup(m => m.GetUserData(_user, item)).Returns(_itemData);
        return item;
    }

    private Task Sync(Audio item, long? listenTs = null) =>
        TestedService.SyncToListenBrainzAsync(item.Id, _user.Id, listenTs, CancellationToken.None);

    private void VerifyFeedbackSent(string? mbid, string? msid) =>
        ListenBrainz.Verify(
            m => m.SendFeedbackAsync(_userConfig, _itemData.IsFavorite, mbid, msid, CancellationToken.None),
            Times.Once);

    private void VerifyNoFeedbackSent() =>
        ListenBrainz.Verify(
            m => m.SendFeedbackAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None),
            Times.Never);
}
