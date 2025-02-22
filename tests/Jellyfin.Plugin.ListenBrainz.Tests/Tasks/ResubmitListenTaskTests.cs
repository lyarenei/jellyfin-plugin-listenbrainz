using System.Collections.Generic;
using System.Net.Http;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks;

public class ResubmitListensTaskTests
{
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<IHttpClientFactory> _clientFactoryMock;
    private readonly Mock<ILibraryManager> _libraryManagerMock;
    private readonly Mock<IUserManager> _userManagerMock;
    private readonly Mock<IListensCacheManager> _listensCacheManagerMock;
    private readonly Mock<IListenBrainzClient> _listenBrainzClientMock;
    private readonly Mock<IMusicBrainzClient> _musicBrainzClientMock;
    private readonly ResubmitListensTask _task;

    public ResubmitListensTaskTests()
    {
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock
            .Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(new NullLogger<ResubmitListensTask>());

        _clientFactoryMock = new Mock<IHttpClientFactory>();
        _libraryManagerMock = new Mock<ILibraryManager>();
        _userManagerMock = new Mock<IUserManager>();
        _listensCacheManagerMock = new Mock<IListensCacheManager>();
        _listenBrainzClientMock = new Mock<IListenBrainzClient>();
        _musicBrainzClientMock = new Mock<IMusicBrainzClient>();

        _task = new ResubmitListensTask(
            _loggerFactoryMock.Object,
            _clientFactoryMock.Object,
            _userManagerMock.Object,
            _libraryManagerMock.Object,
            _listensCacheManagerMock.Object,
            _listenBrainzClientMock.Object,
            _musicBrainzClientMock.Object);
    }

    [Fact]
    public void GetInterval_ReturnValidInterval()
    {
        var interval = ResubmitListensTask.GetInterval();
        // Should be between 24h and 24h50m
        Assert.True(interval > TimeSpan.TicksPerDay);
        Assert.True(interval <= TimeSpan.TicksPerDay + (50 * TimeSpan.TicksPerMinute));
    }

    [Fact]
    public void GetInterval_ConsecutiveCallsReturnDifferentValues()
    {
        var interval1 = ResubmitListensTask.GetInterval();
        var interval2 = ResubmitListensTask.GetInterval();
        Assert.NotEqual(interval1, interval2);
    }

    [Fact]
    public void IsValidListen_ShouldReturnTrueForValidListen()
    {
        var listen = new StoredListen
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345567890
        };

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listen.Id))
            .Returns(new Audio());

        Assert.True(_task.IsValidListen(listen));
    }

    [Fact]
    public void IsValidListen_ShouldReturnFalseForInvalidListen()
    {
        var listen = new StoredListen
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345567890
        };

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listen.Id))
            .Returns(new Movie());

        Assert.False(_task.IsValidListen(listen));
    }

    [Fact]
    public void UpdateMetadataIfNecessary_ShouldNotDoAnythingIfRecordingMbidIsPresent()
    {
        var listen = new StoredListen
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345567890,
            Metadata = new AudioItemMetadata { RecordingMbid = "mbid-not-changed" }
        };

        _task.UpdateMetadataIfNecessary(listen);
        Assert.Equal("mbid-not-changed", listen.Metadata.RecordingMbid);
    }

    [Fact]
    public void UpdateMetadataIfNecessary_ShouldReturnNullIfInvalidListen()
    {
        var listen = new StoredListen
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345567890
        };

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listen.Id))
            .Returns(new Movie());

        Assert.Null(_task.UpdateMetadataIfNecessary(listen));
    }

    [Fact]
    public void UpdateMetadataIfNecessary_ShouldUpdateMetadata()
    {
        var listen = new StoredListen
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345567890,
            Metadata = new AudioItemMetadata { RecordingMbid = "old-mbid" }
        };

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listen.Id))
            .Returns(new Audio());

        _musicBrainzClientMock
            .Setup(mbc => mbc.GetAudioItemMetadata(It.IsAny<Audio>()))
            .Returns(new AudioItemMetadata { RecordingMbid = "new-mbid" });

        _task.UpdateMetadataIfNecessary(listen);
        Assert.Equal("new-mbid", listen.Metadata.RecordingMbid);
    }

    [Fact]
    public async Task ProcessChunkOfStoredListens_Ok()
    {
        var token = CancellationToken.None;
        var listens = new[]
        {
            new StoredListen
            {
                Id = Guid.NewGuid(),
                ListenedAt = 12345567890,
                Metadata = new AudioItemMetadata { RecordingMbid = "mbid-1" }
            },
            new StoredListen
            {
                Id = Guid.NewGuid(),
                ListenedAt = 12345567891,
                Metadata = new AudioItemMetadata { RecordingMbid = "mbid-2" }
            }
        };

        var user = new User("foobar", "auth-provider-id", "pw-reset-provider-id");
        var userConfig = new UserConfig
        {
            JellyfinUserId = user.Id,
            UserName = "foobar",
            IsListenSubmitEnabled = true,
            ApiToken = "some-token",
            PlaintextApiToken = "some-token"
        };

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns(new Audio());

        await _task.ProcessChunkOfStoredListens(listens, userConfig, token);

        _listenBrainzClientMock.Verify(lbc =>
                lbc.SendListensAsync(userConfig,
                    It.Is<IEnumerable<Listen>>(l => l.Count() == 2),
                    token),
            Times.Once);

        _listensCacheManagerMock.Verify(lcm =>
                lcm.RemoveListensAsync(userConfig.JellyfinUserId,
                    It.Is<IEnumerable<StoredListen>>(l => l == listens)),
            Times.Once);

        _listensCacheManagerMock.Verify(lcm => lcm.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ProcessChunkOfStoredListens_DropInvalid()
    {
        var token = CancellationToken.None;
        var listens = new[]
        {
            new StoredListen
            {
                Id = Guid.NewGuid(),
                ListenedAt = 12345567890,
                Metadata = new AudioItemMetadata { RecordingMbid = "mbid-1" }
            },
            new StoredListen
            {
                Id = Guid.NewGuid(),
                ListenedAt = 12345567891,
                Metadata = new AudioItemMetadata { RecordingMbid = "mbid-2" }
            }
        };

        var user = new User("foobar", "auth-provider-id", "pw-reset-provider-id");
        var userConfig = new UserConfig
        {
            JellyfinUserId = user.Id,
            UserName = "foobar",
            IsListenSubmitEnabled = true,
            ApiToken = "some-token",
            PlaintextApiToken = "some-token"
        };

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listens[0].Id))
            .Returns(new Audio());

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listens[1].Id))
            .Returns(new Movie());

        await _task.ProcessChunkOfStoredListens(listens, userConfig, token);

        _listenBrainzClientMock.Verify(lbc =>
                lbc.SendListensAsync(userConfig,
                    It.Is<IEnumerable<Listen>>(l => l.Count() == 1),
                    token),
            Times.Once);

        _listensCacheManagerMock.Verify(lcm =>
                lcm.RemoveListensAsync(userConfig.JellyfinUserId,
                    It.Is<IEnumerable<StoredListen>>(l => l.Count() == 1 && l.First() == listens[0])),
            Times.Once);

        _listensCacheManagerMock.Verify(lcm => lcm.SaveAsync(), Times.Once);
    }
}
