using System;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Tests;

public class ListenBrainzLimitsTest
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
            Assert.Throws<SubmissionConditionsNotMet>(() => Limits.EvaluateSubmitConditions(position, runtime));
        }
        else
        {
            Limits.EvaluateSubmitConditions(position, runtime);
        }
    }
}
