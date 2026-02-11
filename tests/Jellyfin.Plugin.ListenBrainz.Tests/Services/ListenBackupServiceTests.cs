using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Common;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller.Entities.Audio;
using Moq;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public class ListenBackupServiceTests
{
    private static Audio GetAudio(Guid id, string name)
    {
        return new Audio
        {
            Id = id,
            Name = name,
            Artists = ["artist"],
            RunTimeTicks = TimeSpan.FromMinutes(2).Ticks,
        };
    }

    [Fact]
    public async Task Backup_CreatesFileWithListen()
    {
        var backupRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storageMock = new Mock<IPersistentJsonService<List<Listen>>>();
        var userName = "user";
        var audio = GetAudio(Guid.NewGuid(), "song");
        var timestamp = DateUtils.CurrentTimestamp;
        var filePath = Path.Combine(backupRoot, userName, $"{DateUtils.TodayIso}.json");

        storageMock
            .Setup(storage => storage.ReadAsync(filePath, CancellationToken.None))
            .ThrowsAsync(new ServiceException("Failed to read JSON file", new FileNotFoundException()));

        var service = new DefaultListenBackupService(
            new NullLogger<DefaultListenBackupService>(),
            backupRoot,
            storageMock.Object);

        await service.Backup(userName, audio, null, timestamp, CancellationToken.None);

        storageMock.Verify(
            storage => storage.SaveAsync(
                It.Is<List<Listen>>(listens =>
                    listens.Count == 1
                    && listens[0].ListenedAt == timestamp
                    && listens[0].TrackMetadata.TrackName == audio.Name),
                filePath,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Backup_AppendsToExistingFile()
    {
        var backupRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storageMock = new Mock<IPersistentJsonService<List<Listen>>>();
        var userName = "user";
        var audio1 = GetAudio(Guid.NewGuid(), "song1");
        var audio2 = GetAudio(Guid.NewGuid(), "song2");
        var ts1 = DateUtils.CurrentTimestamp;
        var ts2 = ts1 + 1;
        var filePath = Path.Combine(backupRoot, userName, $"{DateUtils.TodayIso}.json");

        storageMock
            .SetupSequence(storage => storage.ReadAsync(filePath, CancellationToken.None))
            .ThrowsAsync(new ServiceException("Failed to read JSON file", new FileNotFoundException()))
            .ReturnsAsync([audio1.AsListen(ts1)]);

        var service = new DefaultListenBackupService(
            new NullLogger<DefaultListenBackupService>(),
            backupRoot,
            storageMock.Object);

        await service.Backup(userName, audio1, null, ts1, CancellationToken.None);
        await service.Backup(userName, audio2, null, ts2, CancellationToken.None);

        storageMock.Verify(
            storage => storage.SaveAsync(
                It.Is<List<Listen>>(listens =>
                    listens.Count == 1
                    && listens[0].ListenedAt == ts1),
                filePath,
                CancellationToken.None),
            Times.Once);
        storageMock.Verify(
            storage => storage.SaveAsync(
                It.Is<List<Listen>>(listens =>
                    listens.Count == 2
                    && listens[0].ListenedAt == ts1
                    && listens[1].ListenedAt == ts2),
                filePath,
                CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task Backup_Throws_WhenExistingJsonIsInvalid()
    {
        var backupRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var storageMock = new Mock<IPersistentJsonService<List<Listen>>>();
        storageMock
            .Setup(storage => storage.ReadAsync(It.IsAny<string?>(), CancellationToken.None))
            .ThrowsAsync(new ServiceException("Failed to read JSON file", new JsonException("Invalid JSON")));
        var service = new DefaultListenBackupService(
            new NullLogger<DefaultListenBackupService>(),
            backupRoot,
            storageMock.Object);
        var audio = GetAudio(Guid.NewGuid(), "song");

        await Assert.ThrowsAsync<ServiceException>(() =>
            service.Backup("user", audio, null, DateUtils.CurrentTimestamp, CancellationToken.None));

        storageMock.Verify(
            storage => storage.SaveAsync(
                It.IsAny<List<Listen>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
