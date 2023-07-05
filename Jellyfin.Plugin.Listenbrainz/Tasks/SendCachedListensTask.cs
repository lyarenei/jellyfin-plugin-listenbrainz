using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Configuration;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.Listenbrainz.Tasks;

/// <summary>
/// Jellyfin scheduled task for re-sending listens stored in cache.
/// </summary>
public class SendCachedListensTask : IScheduledTask
{
    /// <inheritdoc />
    public string Name => "Send cached listens to ListenBrainz";

    /// <inheritdoc />
    public string Key => "SendCachedListens";

    /// <inheritdoc />
    public string Description => "Send listens currently held in listen cache.";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = GetInterval()
            }
        };
    }

    private static long GetInterval()
    {
        var random = new Random();
        var randomMinute = random.Next(50);
        return TimeSpan.TicksPerDay + (randomMinute * TimeSpan.TicksPerMinute);
    }
}
