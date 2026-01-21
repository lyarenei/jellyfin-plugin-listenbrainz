using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

using ListenCacheData = Dictionary<Guid, List<StoredListen>>;

public class ListensCachingServiceTests
{
    private static Audio GetAudio()
    {
        return new Audio
        {
            Name = "track",
            Artists = ["artist"],
        };
    }

    private static DefaultListensCachingService GetService(
        IPersistentJsonService<ListenCacheData>? storage = null,
        bool restore = true)
    {
        var storageMock = new Mock<IPersistentJsonService<ListenCacheData>>();
        return new DefaultListensCachingService(
            new NullLogger<DefaultListensCachingService>(),
            storage ?? storageMock.Object,
            restore);
    }

    [Fact]
    public void Constructor_RestoresCache_WhenReadSucceeds()
    {
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var storedListens = new List<StoredListen>
        {
            new()
            {
                Id = itemId,
                ListenedAt = 123,
            },
        };
        var storedData = new ListenCacheData { [userId] = storedListens };
        var storageMock = new Mock<IPersistentJsonService<ListenCacheData>>();
        storageMock.Setup(storage => storage.Read(It.IsAny<string?>())).Returns(storedData);
        var service = GetService(storageMock.Object);

        var listens = service.GetListens(userId).ToList();
        Assert.Single(listens);
        Assert.Equal(itemId, listens[0].Id);
    }

    [Fact]
    public void AddListen_AddsEntry()
    {
        var service = GetService(null, false);
        var userId = Guid.NewGuid();
        var audio = GetAudio();

        service.AddListen(userId, audio, null, 123);

        var listens = service.GetListens(userId).ToList();
        Assert.Single(listens);
        Assert.Equal(audio.Id, listens[0].Id);
        Assert.Equal(123, listens[0].ListenedAt);
    }

    [Fact]
    public async Task AddListenAsync_AddsEntry()
    {
        var service = GetService(null, false);
        var userId = Guid.NewGuid();
        var audio = GetAudio();

        await service.AddListenAsync(userId, audio, null, 123);

        var listens = service.GetListens(userId).ToList();
        Assert.Single(listens);
        Assert.Equal(audio.Id, listens[0].Id);
    }

    [Fact]
    public void RemoveListen_RemovesMatchingEntry()
    {
        var service = GetService(null, false);
        var userId = Guid.NewGuid();
        var audio = GetAudio();
        var storedListen = new StoredListen
        {
            Id = audio.Id,
            ListenedAt = 123,
        };

        service.AddListen(userId, audio, null, 123);
        service.RemoveListen(userId, storedListen);

        Assert.Empty(service.GetListens(userId));
    }

    [Fact]
    public void RemoveListens_RemovesOnlyMatchingEntries()
    {
        var service = GetService(null, false);
        var userId = Guid.NewGuid();
        var audio1 = GetAudio();
        var audio2 = GetAudio();
        var storedListens = new List<StoredListen>
        {
            new()
            {
                Id = audio1.Id,
                ListenedAt = 123,
            },
        };

        service.AddListen(userId, audio1, null, 123);
        service.AddListen(userId, audio2, null, 1234);
        service.RemoveListens(userId, storedListens);

        var listens = service.GetListens(userId).ToList();
        Assert.Single(listens);
        Assert.Equal(audio2.Id, listens[0].Id);
    }

    [Fact]
    public async Task SaveAsync_PersistsCurrentState()
    {
        var storageMock = new Mock<IPersistentJsonService<ListenCacheData>>();
        var service = GetService(storageMock.Object, restore: false);
        var userId = Guid.NewGuid();
        var audio = GetAudio();

        await service.AddListenAsync(userId, audio, null, 123);
        await service.SaveAsync();

        List<StoredListen>? savedListens;
        storageMock.Verify(
            storage => storage.SaveAsync(
                It.Is<ListenCacheData>(data =>
                    data.TryGetValue(userId, out savedListens) &&
                    savedListens.Count == 1 &&
                    savedListens[0].Id == audio.Id),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
