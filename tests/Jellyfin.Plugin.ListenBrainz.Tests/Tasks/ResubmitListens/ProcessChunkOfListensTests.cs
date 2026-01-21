using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.ResubmitListens;

public class ProcessChunkOfListensTests : TestBase
{
    private static User GetUser() => new("foobar", "auth-provider-id", "pw-reset-provider-id");

    private static UserConfig GetUserConfig(Guid userId) => new()
    {
        JellyfinUserId = userId,
        UserName = "foobar",
        IsListenSubmitEnabled = true,
        ApiToken = "some-token",
        PlaintextApiToken = "some-token",
    };

    [Fact]
    public async Task ShouldFetchMetadata_WhenMbidIsMissing()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id);
        listens[0].Metadata = new AudioItemMetadata { RecordingMbid = string.Empty };
        listens[1].Metadata = new AudioItemMetadata { RecordingMbid = string.Empty };

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns(new Audio());

        _metadataProviderServiceMock
            .Setup(ms => ms.GetAudioItemMetadataAsync(It.IsAny<Audio>(), token))
            .Returns(Task.FromResult(new AudioItemMetadata { RecordingMbid = "fetched-mbid" })!);

        _pluginConfigServiceMock.SetupGet(pc => pc.IsMusicBrainzEnabled).Returns(true);

        _listenBrainzServiceMock
            .Setup(lb => lb.SendListensAsync(userConfig, It.IsAny<IEnumerable<Listen>>(), token))
            .ReturnsAsync(true);

        await _task.ProcessChunkOfListens(listens, userConfig, token);
        Assert.Equal("fetched-mbid", listens[0].Metadata?.RecordingMbid);
        Assert.Equal("fetched-mbid", listens[1].Metadata?.RecordingMbid);

        _metadataProviderServiceMock.Verify(ms => ms.GetAudioItemMetadataAsync(It.IsAny<Audio>(), token),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ShouldNotFetchMetadata_WhenMbidIsPresent()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id);
        listens[0].Metadata = new AudioItemMetadata { RecordingMbid = "fake-mbid1" };
        listens[1].Metadata = new AudioItemMetadata { RecordingMbid = "fake-mbid2" };

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns(new Audio());

        await _task.ProcessChunkOfListens(listens, userConfig, token);
        Assert.Equal("fake-mbid1", listens[0].Metadata?.RecordingMbid);
        Assert.Equal("fake-mbid2", listens[1].Metadata?.RecordingMbid);

        _metadataProviderServiceMock.Verify(ms => ms.GetAudioItemMetadataAsync(It.IsAny<Audio>(), token), Times.Never);
    }

    [Fact]
    public async Task AllOk()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id);

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns(new Audio());

        _listenBrainzServiceMock
            .Setup(lsm =>
                lsm.SendListensAsync(
                    userConfig,
                    It.Is<IEnumerable<Listen>>(l => l.Count() == 2),
                    token))
            .Returns(Task.FromResult(true));

        await _task.ProcessChunkOfListens(listens, userConfig, token);

        _listenBrainzServiceMock.Verify(lbc =>
                lbc.SendListensAsync(
                    userConfig,
                    It.Is<IEnumerable<Listen>>(l => l.Count() == 2),
                    token),
            Times.Once);

        _listensCachingServiceMock.Verify(lcm => lcm.RemoveListensAsync(userConfig.JellyfinUserId, listens), Times.Once);
        _listensCachingServiceMock.Verify(lcm => lcm.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task DropInvalid()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id);

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listens[0].Id))
            .Returns(new Audio());

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listens[1].Id))
            .Returns(new Movie());

        _listenBrainzServiceMock
            .Setup(lsm => lsm.SendListensAsync(
                userConfig,
                It.Is<IEnumerable<Listen>>(l => l.Count() == 1),
                token))
            .Returns(Task.FromResult(true));

        await _task.ProcessChunkOfListens(listens, userConfig, token);

        _listenBrainzServiceMock.Verify(lbc =>
                lbc.SendListensAsync(
                    userConfig,
                    It.Is<IEnumerable<Listen>>(l => l.Count() == 1),
                    token),
            Times.Once);

        _listensCachingServiceMock.Verify(lcm =>
                lcm.RemoveListensAsync(
                    userConfig.JellyfinUserId,
                    It.Is<IEnumerable<StoredListen>>(l => l.Count() == 1 && l.First() == listens[0])),
            Times.Once);

        _listensCachingServiceMock.Verify(lcm => lcm.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ShouldNotRemove_WhenSendFails()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id);

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns(new Audio());

        _listenBrainzServiceMock
            .Setup(lsm => lsm.SendListensAsync(
                userConfig,
                It.Is<IEnumerable<Listen>>(l => l.Count() == 2),
                token))
            .Returns(Task.FromResult(false));

        await _task.ProcessChunkOfListens(listens, userConfig, token);

        _listensCachingServiceMock.Verify(lcm => lcm.RemoveListensAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<StoredListen>>()), Times.Never);
        _listensCachingServiceMock.Verify(lcm => lcm.SaveAsync(), Times.Never);
    }
}
