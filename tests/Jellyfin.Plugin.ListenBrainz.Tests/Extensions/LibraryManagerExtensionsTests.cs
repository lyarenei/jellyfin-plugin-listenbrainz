using System;
using Jellyfin.Plugin.ListenBrainz.Common;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Extensions;

public class LibraryManagerExtensionsTests
{
    private readonly Mock<ILibraryManager> _libraryManagerMock = new();

    [Fact]
    public void ToListen_ItemIsNotAudio()
    {
        var itemId = Guid.NewGuid();
        var notAudio = new Movie { Id = itemId };
        var storedListen = new StoredListen { Id = itemId };
        _libraryManagerMock.Setup(lm => lm.GetItemById(storedListen.Id)).Returns(notAudio);

        var result = _libraryManagerMock.Object.ToListen(storedListen);
        Assert.Null(result);
    }

    [Fact]
    public void ToListen_ItemIsAudio()
    {
        var itemId = Guid.NewGuid();
        var audio = new Audio { Id = itemId };
        var listenTs = DateUtils.CurrentTimestamp;
        var storedListen = new StoredListen
        {
            Id = itemId,
            ListenedAt = listenTs
        };

        _libraryManagerMock.Setup(lm => lm.GetItemById(storedListen.Id)).Returns(audio);

        var result = _libraryManagerMock.Object.ToListen(storedListen);
        Assert.NotNull(result);
    }
}
