using System;
using Jellyfin.Plugin.ListenBrainz.Api.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Api.Tests;

public class LimitsTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(30, 40, false)]
    [InlineData(30, 90, true)]
    [InlineData(4 * TimeSpan.TicksPerMinute, TimeSpan.TicksPerHour, false)]
    public void ListenBrainzLimits_EvaluateSubmitConditions(long position, long runtime, bool throws)
    {
        if (throws)
        {
            Assert.Throws<ListenBrainzException>(() => Limits.AssertSubmitConditions(position, runtime));
        }
        else
        {
            Limits.AssertSubmitConditions(position, runtime);
        }
    }
}
