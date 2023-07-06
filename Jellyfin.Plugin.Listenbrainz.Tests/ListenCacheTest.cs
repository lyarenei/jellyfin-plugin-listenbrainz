using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Services.ListenCache;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Tests;

public class ListenCacheTest
{
    private static readonly Mock<ILogger<DefaultListenCache>> _loggerMock = new();

    private readonly LbUser _exampleUser = new()
    {
        Name = "exampleUser",
        Token = "no_token",
        MediaBrowserUserId = new Guid(),
        Options = new UserOptions()
    };

    private readonly Listen _exampleListen = new()
    {
        ListenedAt = 1234,
        Data = new TrackMetadata
        {
            ArtistName = "Foo Bar",
            TrackName = "Foo - Bar"
        }
    };

    private readonly Listen _exampleListenDifferentTs = new()
    {
        ListenedAt = 9876,
        Data = new TrackMetadata
        {
            ArtistName = "Foo Bar",
            TrackName = "Foo - Bar"
        }
    };

    private readonly Listen _exampleListen2 = new()
    {
        ListenedAt = 6789,
        Data = new TrackMetadata
        {
            ArtistName = "Foo Baz",
            TrackName = "Foo - Baz"
        }
    };

    [Fact]
    public void ListenCache_AddListenUserNotExists()
    {
        var cache = new DefaultListenCache(string.Empty, _loggerMock.Object);
        cache.Add(_exampleUser, _exampleListen);

        var gotListens = cache.Get(_exampleUser);
        Assert.Equal(new Collection<Listen> { _exampleListen }, gotListens);
    }

    [Fact]
    public void ListenCache_AddListenUserExists()
    {
        var cacheData = new Dictionary<string, List<Listen>> { { _exampleUser.Name, new List<Listen> { _exampleListen } } };
        var cache = new DefaultListenCache(string.Empty, cacheData, _loggerMock.Object);
        cache.Add(_exampleUser, _exampleListen);

        var expectedListens = new List<Listen>
        {
            _exampleListen,
            _exampleListen
        };
        var gotListens = cache.Get(_exampleUser);
        Assert.Equal(expectedListens, gotListens);
    }

    [Fact]
    public void ListenCache_RemoveListen()
    {
        var cacheData = new Dictionary<string, List<Listen>> { { _exampleUser.Name, new List<Listen> { _exampleListen } } };
        var cache = new DefaultListenCache(string.Empty, cacheData, _loggerMock.Object);
        cache.Remove(_exampleUser, new List<Listen> { _exampleListen });

        var userListens = cache.Get(_exampleUser);
        Assert.False(userListens.Any());
    }

    [Fact]
    public void ListenCache_RemoveListenNotExists()
    {
        var cacheData = new Dictionary<string, List<Listen>> { { _exampleUser.Name, new List<Listen> { _exampleListen } } };
        var cache = new DefaultListenCache(string.Empty, cacheData, _loggerMock.Object);
        cache.Remove(_exampleUser, new List<Listen> { _exampleListen2 });

        var userListens = cache.Get(_exampleUser);
        Assert.True(userListens.Any());
    }

    [Fact]
    public void ListenCache_RemoveListenEmpty()
    {
        var cacheData = new Dictionary<string, List<Listen>> { { _exampleUser.Name, new List<Listen>() } };
        var cache = new DefaultListenCache(string.Empty, cacheData, _loggerMock.Object);
        cache.Remove(_exampleUser, new List<Listen> { _exampleListen });

        var userListens = cache.Get(_exampleUser);
        Assert.False(userListens.Any());
    }

    [Fact]
    public void ListenCache_RemoveListenWithDifferentTs()
    {
        var cacheData = new Dictionary<string, List<Listen>>
        {
            {
                _exampleUser.Name, new List<Listen>
                {
                    _exampleListen,
                    _exampleListenDifferentTs
                }
            }
        };
        var cache = new DefaultListenCache(string.Empty, cacheData, _loggerMock.Object);
        cache.Remove(_exampleUser, new List<Listen> { _exampleListen });

        var userListens = cache.Get(_exampleUser);
        Assert.Equal(new List<Listen> { _exampleListenDifferentTs }, userListens);
    }
}
