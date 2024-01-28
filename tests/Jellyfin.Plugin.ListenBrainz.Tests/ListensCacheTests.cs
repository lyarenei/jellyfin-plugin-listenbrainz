using System;
using System.Linq;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Managers;
using MediaBrowser.Controller.Entities.Audio;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests;

public class ListensCacheTests
{
    private static readonly UserConfig _exampleUser = new()
    {
        UserName = "foobar",
        JellyfinUserId = new Guid()
    };

    private static readonly Audio _exampleAudio = new();
    private readonly ListensCacheManager _cache;

    public ListensCacheTests()
    {
        _cache = new ListensCacheManager(string.Empty, false);
    }

    [Fact]
    public void ListenCache_AddListen()
    {
        const int Ts = 10;
        _cache.AddListen(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts);

        var gotListens = _cache.GetListens(_exampleUser.JellyfinUserId).ToList();
        Assert.Collection(gotListens, listen => Assert.Equal(Ts, listen.ListenedAt));
    }

    [Fact]
    public async void ListenCache_AddListenAsync()
    {
        const int Ts = 10;
        await _cache.AddListenAsync(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts);

        var gotListens = _cache.GetListens(_exampleUser.JellyfinUserId).ToList();
        Assert.Collection(gotListens, listen => Assert.Equal(Ts, listen.ListenedAt));
    }

    [Fact]
    public void ListenCache_AddListenDuplicate()
    {
        const int Ts = 10;

        _cache.AddListen(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts);
        _cache.AddListen(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts);

        var gotListens = _cache.GetListens(_exampleUser.JellyfinUserId).ToList();
        Assert.Collection(
            gotListens,
            listen => Assert.Equal(Ts, listen.ListenedAt), listen => Assert.Equal(Ts, listen.ListenedAt));
        Assert.Equal(2, gotListens.Count());
    }

    [Fact]
    public async void ListenCache_AddListenDuplicateAsync()
    {
        const int Ts = 10;

        await _cache.AddListenAsync(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts);
        await _cache.AddListenAsync(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts);

        var gotListens = _cache.GetListens(_exampleUser.JellyfinUserId).ToList();
        Assert.Collection(
            gotListens,
            listen => Assert.Equal(Ts, listen.ListenedAt), listen => Assert.Equal(Ts, listen.ListenedAt));
        Assert.Equal(2, gotListens.Count());
    }

    [Fact]
    public void ListenCache_RemoveListen()
    {
        const int Ts = 10;
        _cache.AddListen(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts);

        var storedListen = new StoredListen
        {
            Id = _exampleAudio.Id,
            ListenedAt = Ts
        };
        _cache.RemoveListens(_exampleUser.JellyfinUserId, new[] { storedListen });

        var gotListens = _cache.GetListens(_exampleUser.JellyfinUserId);
        Assert.Empty(gotListens);
    }

    [Fact]
    public void ListenCache_RemoveListenNotExists()
    {
        _cache.AddListen(_exampleUser.JellyfinUserId, _exampleAudio, null, 10);

        var storedListen = new StoredListen
        {
            Id = _exampleAudio.Id,
            ListenedAt = 11
        };
        _cache.RemoveListens(_exampleUser.JellyfinUserId, new[] { storedListen });

        var gotListens = _cache.GetListens(_exampleUser.JellyfinUserId);
        Assert.NotEmpty(gotListens);
    }

    [Fact]
    public void ListenCache_RemoveListenWithDifferentTs()
    {
        const int Ts1 = 10;
        const int Ts2 = 11;
        _cache.AddListen(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts1);
        _cache.AddListen(_exampleUser.JellyfinUserId, _exampleAudio, null, Ts2);

        var storedListen = new StoredListen
        {
            Id = _exampleAudio.Id,
            ListenedAt = Ts2
        };
        _cache.RemoveListens(_exampleUser.JellyfinUserId, new[] { storedListen });

        var gotListens = _cache.GetListens(_exampleUser.JellyfinUserId).ToList();
        Assert.Collection(gotListens, listen => Assert.Equal(Ts1, listen.ListenedAt));
    }
}
