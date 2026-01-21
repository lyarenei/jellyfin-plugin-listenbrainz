using System;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.ResubmitListens;

public class IsValidListenTests : TestBase
{
    [Fact]
    public void ShouldReturnTrue_WhenValidListen()
    {
        var listen = GetStoredListens()[0];

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listen.Id))
            .Returns(new Audio());

        Assert.True(_task.IsValidListen(listen));
    }

    [Fact]
    public void ShouldReturnFalse_WhenInvalidListen()
    {
        var listen = GetStoredListens()[0];

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listen.Id))
            .Returns(new Movie());

        Assert.False(_task.IsValidListen(listen));
    }

    [Fact]
    public void ShouldCatchException()
    {
        var listen = GetStoredListens()[0];

        _libraryManagerMock
            .Setup(lm => lm.GetItemById(listen.Id))
            .Throws<Exception>();

        Assert.False(_task.IsValidListen(listen));
    }

}
