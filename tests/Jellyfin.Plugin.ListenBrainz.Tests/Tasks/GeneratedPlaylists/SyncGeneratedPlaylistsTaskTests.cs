using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Tasks.SyncGeneratedPlaylists;
using Jellyfin.Plugin.ListenBrainz.Tests.TestKit;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.GeneratedPlaylists;

public class SyncGeneratedPlaylistsTaskTests : ServiceTest<SyncGeneratedPlaylistsTask>
{
    private readonly UserConfig _firstUser = EnabledUserConfig();
    private readonly UserConfig _secondUser = EnabledUserConfig();

    public SyncGeneratedPlaylistsTaskTests()
    {
        PluginConfig
            .Setup(m => m.UserConfigs)
            .Returns([_firstUser, _secondUser]);

        UserManager
            .Setup(m => m.GetUserById(It.IsAny<Guid>()))
            .Returns(TestData.User());

        StateService
            .Setup(m => m.ReadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlaylistSyncState());
    }

    private Mock<IPluginConfigService> PluginConfig => MockOf<IPluginConfigService>();

    private Mock<IUserManager> UserManager => MockOf<IUserManager>();

    private Mock<IPlaylistDiscoveryService> Discovery => MockOf<IPlaylistDiscoveryService>();

    private Mock<IPlaylistSyncStateService> StateService => MockOf<IPlaylistSyncStateService>();

    [Fact]
    public async Task ExecuteAsync_SyncsRemainingUsers_WhenDiscoveryFailsForOneUser()
    {
        GivenDiscoveryFails(_firstUser);
        GivenNoPlaylists(_secondUser);

        await TestedService.ExecuteAsync(Mock.Of<IProgress<double>>(), CancellationToken.None);

        VerifyDiscoveryRequested(_secondUser);
    }

    private static UserConfig EnabledUserConfig()
    {
        var config = TestData.UserConfig();
        config.IsGeneratedPlaylistsSyncEnabled = true;
        return config;
    }

    private void GivenDiscoveryFails(UserConfig config) => Discovery
        .Setup(m => m.DiscoverAsync(config, It.IsAny<CancellationToken>()))
        .ReturnsAsync(PlaylistDiscoveryResult.Failed);

    private void GivenNoPlaylists(UserConfig config) => Discovery
        .Setup(m => m.DiscoverAsync(config, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new PlaylistDiscoveryResult([], true));

    private void VerifyDiscoveryRequested(UserConfig config) => Discovery
        .Verify(m => m.DiscoverAsync(config, It.IsAny<CancellationToken>()), Times.Once);
}
