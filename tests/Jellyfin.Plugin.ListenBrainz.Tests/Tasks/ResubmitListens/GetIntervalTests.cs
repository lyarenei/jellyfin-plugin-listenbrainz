using System;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.ResubmitListens;

public class GetIntervalTests
{
    [Fact]
    public void ReturnValidInterval()
    {
        var interval = ResubmitListensTask.GetInterval();
        // Should be between 24h and 24h50m
        Assert.True(interval > TimeSpan.TicksPerDay);
        Assert.True(interval <= TimeSpan.TicksPerDay + (50 * TimeSpan.TicksPerMinute));
    }

    [Fact]
    public void ReturnValidIntervals_WhenConsecutiveCalls()
    {
        var intervals = new[]
        {
            ResubmitListensTask.GetInterval(),
            ResubmitListensTask.GetInterval(),
            ResubmitListensTask.GetInterval(),
        };

        foreach (var interval in intervals)
        {
            Assert.True(interval > TimeSpan.TicksPerDay);
            Assert.True(interval <= TimeSpan.TicksPerDay + (50 * TimeSpan.TicksPerMinute));
        }
    }
}
