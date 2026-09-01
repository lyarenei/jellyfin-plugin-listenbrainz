using System;
using System.IO;
using System.Text.Json;
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
    private static DefaultPlaylistSyncStateService ServiceReading(Func<Task<PlaylistSyncState>> read)
    {
        var storage = new Mock<IPersistentJsonService<PlaylistSyncState>>();
        storage
            .Setup(s => s.ReadAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(read);

        return new DefaultPlaylistSyncStateService(NullLogger.Instance, storage.Object);
    }

    [Fact]
    public async Task ReadAsync_ReturnsEmptyState_WhenStateFileDoesNotExist()
    {
        var service = ServiceReading(() =>
            Task.FromException<PlaylistSyncState>(
                new ServiceException("missing file", new FileNotFoundException())));

        var state = await service.ReadAsync(CancellationToken.None);

        Assert.NotNull(state);
        Assert.Empty(state.Entries);
    }

    [Fact]
    public async Task ReadAsync_ReturnsEmptyState_WhenStateFileIsCorrupt()
    {
        var service = ServiceReading(() =>
            Task.FromException<PlaylistSyncState>(
                new ServiceException("corrupt file", new JsonException("unexpected token"))));

        var state = await service.ReadAsync(CancellationToken.None);

        Assert.NotNull(state);
        Assert.Empty(state.Entries);
    }

    [Fact]
    public async Task ReadAsync_Throws_WhenStateFileIsUnreadable()
    {
        var service = ServiceReading(() =>
            Task.FromException<PlaylistSyncState>(
                new ServiceException("unreadable file", new IOException())));

        await Assert.ThrowsAsync<ServiceException>(() => service.ReadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_ReturnsEmptyState_WhenVersionDoesNotMatch()
    {
        var stored = new PlaylistSyncState { Version = PlaylistSyncState.CurrentVersion + 1 };
        stored.Entries.Add(new PlaylistSyncEntry { ListenBrainzPlaylistId = "mbid" });

        var service = ServiceReading(() => Task.FromResult(stored));

        var state = await service.ReadAsync(CancellationToken.None);

        Assert.Empty(state.Entries);
        Assert.Equal(PlaylistSyncState.CurrentVersion, state.Version);
    }

    [Fact]
    public async Task ReadAsync_ReturnsStoredState()
    {
        var stored = new PlaylistSyncState();
        stored.Entries.Add(new PlaylistSyncEntry
        {
            ListenBrainzPlaylistId = "mbid",
            Origin = PlaylistOrigin.Generated,
            GeneratedType = "Jams",
        });

        var service = ServiceReading(() => Task.FromResult(stored));

        var state = await service.ReadAsync(CancellationToken.None);

        Assert.Single(state.Entries);
        Assert.Equal("mbid", state.Entries[0].ListenBrainzPlaylistId);
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
