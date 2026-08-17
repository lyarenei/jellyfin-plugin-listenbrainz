using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Api.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Dtos;
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

    /// <summary>
    /// Report a playback position, as a progress event would while a track plays.
    /// </summary>
    private Task<bool> ReportPosition(Audio audio, int atSecond)
    {
        return _service.UpdatePositionAsync(
            UserId,
            audio.Id.ToString(),
            TimeSpan.FromSeconds(atSecond).Ticks,
            CancellationToken.None);
    }

    private async Task<TrackedItem> Track(Audio audio)
    {
        await _service.AddItemAsync(UserId, audio, CancellationToken.None);
        var tracked = await _service.GetItemAsync(UserId, audio.Id.ToString(), CancellationToken.None);
        Assert.NotNull(tracked);
        return tracked;
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

    [Fact]
    public async Task UpdatePosition_RemembersLastReportedPosition()
    {
        var audio = GetAudio();
        var tracked = await Track(audio);

        await ReportPosition(audio, 10);
        await ReportPosition(audio, 20);
        await ReportPosition(audio, 30);

        Assert.Equal(TimeSpan.FromSeconds(30).Ticks, tracked.PositionTicks);
    }

    [Fact]
    public async Task UpdatePosition_ReturnsFalse_WhenItemIsNotTracked()
    {
        var audio = GetAudio();

        var updated = await ReportPosition(audio, 10);

        Assert.False(updated);
    }

    [Fact]
    public async Task UpdatePosition_ReturnsFalse_WhenTrackingIsInvalidated()
    {
        var audio = GetAudio();
        var tracked = await Track(audio);

        await _service.InvalidateItemAsync(UserId, tracked, CancellationToken.None);
        var updated = await ReportPosition(audio, 10);

        Assert.False(updated);
        Assert.Equal(0, tracked.PositionTicks);
    }

    [Fact]
    public async Task UpdatePosition_ResetsPosition_WhenItemIsRetracked()
    {
        var audio = GetAudio();
        await Track(audio);
        await ReportPosition(audio, 30);

        var second = await Track(audio);

        Assert.Equal(0, second.PositionTicks);
    }

    /// <summary>
    /// Regression test for listens submitted for tracks which were barely played. Alternative
    /// mode used to validate submit conditions against the wall clock since playback started,
    /// which counted time the user spent paused.
    /// </summary>
    [Fact]
    public async Task Position_FailsSubmitConditions_ForTrackSkippedAfterLongPause()
    {
        var audio = GetAudio();
        var runtime = audio.RunTimeTicks!.Value;
        var tracked = await Track(audio);

        // Play 20 seconds, then pause - the position stops advancing no matter how long
        // the user is away before skipping the track.
        await ReportPosition(audio, 20);

        // Elapsed wall clock would clear the 50% bar and submit a listen.
        Limits.AssertSubmitConditions(TimeSpan.FromMinutes(10).Ticks, runtime);

        // The position actually reached does not.
        Assert.Equal(TimeSpan.FromSeconds(20).Ticks, tracked.PositionTicks);
        Assert.Throws<ListenBrainzException>(() => Limits.AssertSubmitConditions(tracked.PositionTicks, runtime));
    }
}
