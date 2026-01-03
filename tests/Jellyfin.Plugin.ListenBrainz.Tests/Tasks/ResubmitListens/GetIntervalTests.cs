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
    public void ReturnDifferentValues_WhenConsecutiveCalls()
    {
        var interval1 = ResubmitListensTask.GetInterval();
        var interval2 = ResubmitListensTask.GetInterval();
        Assert.NotEqual(interval1, interval2);
    }
}
