using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller.Entities.Audio;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public class PlaybackTrackingServiceTests
{
    private const string UserId = "test-user";
    private readonly IPlaybackTrackingService _service;

    public PlaybackTrackingServiceTests()
    {
        _service = new DefaultPlaybackTrackingService();
    }

    private static Audio GetAudio()
    {
        return new Audio
        {
            Name = "track",
            Artists = ["artist"],
            RunTimeTicks = TimeSpan.FromMinutes(2).Ticks,
        };
    }

    [Fact]
    public async Task ShouldReturnTrackedItem_WhenAdded()
    {
        var audio = GetAudio();

        await _service.AddItemAsync(UserId, audio, CancellationToken.None);
        var gotItem = await _service.GetItemAsync(UserId, audio.Id.ToString(), CancellationToken.None);

        Assert.NotNull(gotItem);
        Assert.Equal(UserId, gotItem.UserId);
        Assert.Equal(audio.Id.ToString(), gotItem.ItemId);
        Assert.True(gotItem.IsValid);
    }

    [Fact]
    public async Task ShouldReplaceItem_WhenAlreadyTracked()
    {
        var audio = GetAudio();

        await _service.AddItemAsync(UserId, audio, CancellationToken.None);
        var first = await _service.GetItemAsync(UserId, audio.Id.ToString(), CancellationToken.None);

        await _service.AddItemAsync(UserId, audio, CancellationToken.None);
        var second = await _service.GetItemAsync(UserId, audio.Id.ToString(), CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task ShouldRemoveTrackedItem_WhenItemExists()
    {
        var audio = GetAudio();

        await _service.AddItemAsync(UserId, audio, CancellationToken.None);
        var tracked = await _service.GetItemAsync(UserId, audio.Id.ToString(), CancellationToken.None);

        Assert.NotNull(tracked);
        await _service.RemoveItemAsync(UserId, tracked, CancellationToken.None);

        var after = await _service.GetItemAsync(UserId, audio.Id.ToString(), CancellationToken.None);
        Assert.Null(after);
    }

    [Fact]
    public async Task ShouldInvalidateItem_WhenItemExists()
    {
        var audio = GetAudio();

        await _service.AddItemAsync(UserId, audio, CancellationToken.None);
        var tracked = await _service.GetItemAsync(UserId, audio.Id.ToString(), CancellationToken.None);

        Assert.NotNull(tracked);
        await _service.InvalidateItemAsync(UserId, tracked, CancellationToken.None);

        var after = await _service.GetItemAsync(UserId, audio.Id.ToString(), CancellationToken.None);
        Assert.NotNull(after);
        Assert.False(after.IsValid);
    }

    [Fact]
    public async Task GetItem_ReturnsNull_ForDifferentUser()
    {
        var audio = GetAudio();

        await _service.AddItemAsync(UserId, audio, CancellationToken.None);

        var tracked = await _service.GetItemAsync("another-user", audio.Id.ToString(), CancellationToken.None);
        Assert.Null(tracked);
    }
}
