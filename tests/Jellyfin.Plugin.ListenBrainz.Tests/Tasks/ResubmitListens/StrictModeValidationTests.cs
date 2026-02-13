using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using MediaBrowser.Controller.Entities.Audio;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.ResubmitListens;

public class StrictModeValidationTests : TestBase
{
    private static User GetUser() => new("foobar", "auth-provider-id", "pw-reset-provider-id");

    private static UserConfig GetUserConfig(Guid userId, bool isStrictModeEnabled) => new()
    {
        JellyfinUserId = userId,
        UserName = "foobar",
        IsListenSubmitEnabled = true,
        IsStrictModeEnabled = isStrictModeEnabled,
        ApiToken = "some-token",
        PlaintextApiToken = "some-token",
    };

    [Fact]
    public async Task ShouldSendListen_WhenStrictModeDisabled()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id, isStrictModeEnabled: false);

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns(new Audio());

        _listenBrainzServiceMock
            .Setup(lb => lb.SendListensAsync(userConfig, It.IsAny<IEnumerable<Listen>>(), token))
            .ReturnsAsync(true);

        await _task.ProcessChunkOfListens(listens, userConfig, token);

        _validationServiceMock.Verify(
            vs => vs.ValidateStrictModeConditions(It.IsAny<Audio>()),
            Times.Never);

        _listenBrainzServiceMock.Verify(
            lb => lb.SendListensAsync(
                userConfig,
                It.Is<IEnumerable<Listen>>(l => l.Count() == 2),
                token),
            Times.Once);
    }

    [Fact]
    public async Task ShouldSendListen_WhenStrictModeEnabledAndValid()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id, isStrictModeEnabled: true);

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns(new Audio());

        _validationServiceMock
            .Setup(vs => vs.ValidateStrictModeConditions(It.IsAny<Audio>()));

        _listenBrainzServiceMock
            .Setup(lb => lb.SendListensAsync(userConfig, It.IsAny<IEnumerable<Listen>>(), token))
            .ReturnsAsync(true);

        await _task.ProcessChunkOfListens(listens, userConfig, token);

        _validationServiceMock.Verify(
            vs => vs.ValidateStrictModeConditions(It.IsAny<Audio>()),
            Times.Exactly(2));

        _listenBrainzServiceMock.Verify(
            lb => lb.SendListensAsync(
                userConfig,
                It.Is<IEnumerable<Listen>>(l => l.Count() == 2),
                token),
            Times.Once);
    }

    [Fact]
    public async Task ShouldSkipInvalidListen_WhenStrictModeValidationFails()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id, isStrictModeEnabled: true);

        var validItem = new Audio();
        var invalidItem = new Audio();

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listens[0].Id))
            .Returns(validItem);

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listens[1].Id))
            .Returns(invalidItem);

        _validationServiceMock
            .Setup(vs => vs.ValidateStrictModeConditions(It.Is<Audio>(a => ReferenceEquals(a, invalidItem))))
            .Throws(new ValidationException("Missing MBID"));

        _listenBrainzServiceMock
            .Setup(lb => lb.SendListensAsync(userConfig, It.IsAny<IEnumerable<Listen>>(), token))
            .ReturnsAsync(true);

        await _task.ProcessChunkOfListens(listens, userConfig, token);

        _listensCachingServiceMock.Verify(
            lc => lc.RemoveListensAsync(
                userConfig.JellyfinUserId,
                It.Is<IEnumerable<StoredListen>>(l => l.Count() == 1 && l.First() == listens[0])),
            Times.Once);

        _listenBrainzServiceMock.Verify(
            lb => lb.SendListensAsync(
                userConfig,
                It.Is<IEnumerable<Listen>>(l => l.Count() == 1),
                token),
            Times.Once);
    }

    [Fact]
    public async Task ShouldNotRemoveFromCache_WhenAllFailStrictModeValidation()
    {
        var token = CancellationToken.None;
        var listens = GetStoredListens();
        var user = GetUser();
        var userConfig = GetUserConfig(user.Id, isStrictModeEnabled: true);

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(It.IsAny<Guid>()))
            .Returns(new Audio());

        _validationServiceMock
            .Setup(vs => vs.ValidateStrictModeConditions(It.IsAny<Audio>()))
            .Throws(new ValidationException("Missing MBID"));

        await _task.ProcessChunkOfListens(listens, userConfig, token);

        _listensCachingServiceMock.Verify(
            lc => lc.RemoveListensAsync(
                It.IsAny<Guid>(),
                It.IsAny<IEnumerable<StoredListen>>()),
            Times.Never);

        _listenBrainzServiceMock.Verify(
            lb => lb.SendListensAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<IEnumerable<Listen>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void IsStrictModeValid_ShouldReturnTrue_WhenValidationPasses()
    {
        var item = new Audio();
        var listen = new StoredListen
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345
        };

        _validationServiceMock
            .Setup(vs => vs.ValidateStrictModeConditions(item));

        var result = _task.IsStrictModeValid(item, listen);

        Assert.True(result);
        _validationServiceMock.Verify(
            vs => vs.ValidateStrictModeConditions(item),
            Times.Once);
    }

    [Fact]
    public void IsStrictModeValid_ShouldReturnFalse_WhenValidationFails()
    {
        var item = new Audio();
        var listen = new StoredListen
        {
            Id = Guid.NewGuid(),
            ListenedAt = 12345
        };

        _validationServiceMock
            .Setup(vs => vs.ValidateStrictModeConditions(item))
            .Throws(new ValidationException("Missing MBID"));

        var result = _task.IsStrictModeValid(item, listen);

        Assert.False(result);
        _validationServiceMock.Verify(
            vs => vs.ValidateStrictModeConditions(item),
            Times.Once);
    }
}
