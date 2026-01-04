using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller.Entities.Audio;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

using ListenCacheData = Dictionary<Guid, List<StoredListen>>;

public class ListensCachingServiceTests
{
    private sealed class DummyPersistentJsonService : IPersistentJsonService<ListenCacheData>
    {
        public ListenCacheData? ReadData { get; set; }

        public ListenCacheData? LastSavedData { get; private set; }

        public int SaveAsyncCalls { get; private set; }

        public void Save(ListenCacheData data) => LastSavedData = Clone(data);

        public Task SaveAsync(ListenCacheData data)
        {
            SaveAsyncCalls++;
            LastSavedData = Clone(data);
            return Task.CompletedTask;
        }

        public ListenCacheData Read() => ReadData ?? new ListenCacheData();

        public Task<ListenCacheData> ReadAsync() => Task.FromResult(Read());

        private static ListenCacheData Clone(ListenCacheData data)
        {
            return data.ToDictionary(
                pair => pair.Key,
                pair => pair.Value
                    .Select(sl => new StoredListen
                    {
                        Id = sl.Id,
                        ListenedAt = sl.ListenedAt,
                        Metadata = sl.Metadata,
                    })
                    .ToList());
        }
    }

    private static Audio GetAudio()
    {
        return new Audio
        {
            Name = "track",
            Artists = ["artist"],
        };
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
        var storage = new DummyPersistentJsonService { ReadData = storedData };
        var service = new DefaultListensCachingService(storage);

        var listens = service.GetListens(userId).ToList();
        Assert.Single(listens);
        Assert.Equal(itemId, listens[0].Id);
    }

    [Fact]
    public void AddListen_AddsEntry()
    {
        var storage = new DummyPersistentJsonService();
        var service = new DefaultListensCachingService(storage, restore: false);
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
        var storage = new DummyPersistentJsonService();
        var service = new DefaultListensCachingService(storage, restore: false);
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
        var storage = new DummyPersistentJsonService();
        var service = new DefaultListensCachingService(storage, restore: false);
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
        var storage = new DummyPersistentJsonService();
        var service = new DefaultListensCachingService(storage, restore: false);
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
        var storage = new DummyPersistentJsonService();
        var service = new DefaultListensCachingService(storage, restore: false);
        var userId = Guid.NewGuid();
        var audio = GetAudio();

        await service.AddListenAsync(userId, audio, null, 123);
        await service.SaveAsync();

        Assert.Equal(1, storage.SaveAsyncCalls);
        Assert.NotNull(storage.LastSavedData);
        Assert.True(storage.LastSavedData.TryGetValue(userId, out var savedListens));
        Assert.Single(savedListens);
        Assert.Equal(audio.Id, savedListens[0].Id);
    }
}
