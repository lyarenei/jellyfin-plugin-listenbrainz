using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public class PlaylistSyncStateServiceTests
{
    [Fact]
    public async Task ReadAsync_ReturnsEmptyState_WhenStateFileDoesNotExist()
    {
        var storage = new Mock<IPersistentJsonService<PlaylistSyncState>>();
        storage
            .Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceException("missing file", new FileNotFoundException()));

        var service = new DefaultPlaylistSyncStateService(NullLogger.Instance, storage.Object);

        var state = await service.ReadAsync(CancellationToken.None);

        Assert.NotNull(state);
        Assert.Empty(state.Mappings);
    }

    [Fact]
    public async Task ReadAsync_Throws_WhenStateFileIsUnreadable()
    {
        var storage = new Mock<IPersistentJsonService<PlaylistSyncState>>();
        storage
            .Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceException("corrupt file", new IOException()));

        var service = new DefaultPlaylistSyncStateService(NullLogger.Instance, storage.Object);

        await Assert.ThrowsAsync<ServiceException>(() => service.ReadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_ReturnsStoredState()
    {
        var stored = new PlaylistSyncState();
        stored.Mappings.Add(new PlaylistMapping
        {
            ListenBrainzPlaylistId = "mbid",
            Category = "Jams",
        });

        var storage = new Mock<IPersistentJsonService<PlaylistSyncState>>();
        storage
            .Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stored);

        var service = new DefaultPlaylistSyncStateService(NullLogger.Instance, storage.Object);

        var state = await service.ReadAsync(CancellationToken.None);

        Assert.Single(state.Mappings);
        Assert.Equal("mbid", state.Mappings[0].ListenBrainzPlaylistId);
    }

    [Fact]
    public async Task SaveAsync_DelegatesToStorage()
    {
        var storage = new Mock<IPersistentJsonService<PlaylistSyncState>>();
        var service = new DefaultPlaylistSyncStateService(NullLogger.Instance, storage.Object);
        var state = new PlaylistSyncState();

        await service.SaveAsync(state, CancellationToken.None);

        storage.Verify(
            s => s.SaveAsync(state, It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
