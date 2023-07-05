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
        throw new NotImplementedException();
    }

    private PluginConfiguration GetPluginConfig()
    {
        var config = Plugin.Instance?.Configuration;
        if (config == null)
        {
            throw new PluginInstanceException();
        }

        return config;
    }
}
