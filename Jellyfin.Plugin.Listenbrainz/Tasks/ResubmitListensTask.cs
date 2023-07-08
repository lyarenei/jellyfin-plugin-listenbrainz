using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Clients;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Services;
using Jellyfin.Plugin.Listenbrainz.Services.ListenCache;
using Jellyfin.Plugin.Listenbrainz.Utils;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz.Tasks;

/// <summary>
/// Jellyfin scheduled task for re-sending listens stored in cache.
/// </summary>
public class ResubmitListensTask : IScheduledTask
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ResubmitListensTask> _logger;
    private readonly IListenCache _listenCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResubmitListensTask"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public ResubmitListensTask(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ResubmitListensTask>();
        _listenCache = new DefaultListenCache(
            Helpers.GetListenCacheFilePath(),
            loggerFactory.CreateLogger<DefaultListenCache>());
    }

    /// <inheritdoc />
    public string Name => "Resubmit listens";

    /// <inheritdoc />
    public string Key => "ResubmitListens";

    /// <inheritdoc />
    public string Description => "Resubmit listens in cache to ListenBrainz.";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.GetConfiguration();
        var mbClient = GetMusicBrainzClient();
        var lbClient = GetListenBrainzClient(mbClient);
        await _listenCache.LoadFromFile();

        try
        {
            foreach (var user in config.LbUsers)
            {
                var userListens = _listenCache.Get(user);
                if (userListens.Any())
                {
                    _logger.LogInformation("Found listens in cache for user {Username}, will try resubmitting", user.Name);
                    // TODO: Submit all, not just first X
                    var subset = userListens.Take(Limits.MaxListensPerRequest).ToList();
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        lbClient.SubmitListens(user, subset);
                        _listenCache.Remove(user, subset);
                        await _listenCache.Save();
                    }
                    catch (ListenSubmitException)
                    {
                        _logger.LogInformation("Failed to resubmit listens for user {User}", user.Name);
                    }
                }
                else
                {
                    _logger.LogInformation("User {Username} does not have any cached listens, skipping", user.Name);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Listen resubmitting has been cancelled");
        }
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

    private IMusicbrainzClientService GetMusicBrainzClient()
    {
        var config = Plugin.GetConfiguration();
        if (!config.GlobalConfig.MusicbrainzEnabled)
        {
            return new DummyMusicbrainzClient(_loggerFactory.CreateLogger<DummyMusicbrainzClient>());
        }

        var logger = _loggerFactory.CreateLogger<MusicbrainzClient>();
        return new MusicbrainzClient(config.MusicBrainzUrl(), _httpClientFactory, logger, new SleepService());
    }

    private ListenbrainzClient GetListenBrainzClient(IMusicbrainzClientService mbClient)
    {
        var config = Plugin.GetConfiguration();
        var logger = _loggerFactory.CreateLogger<ListenbrainzClient>();
        return new ListenbrainzClient(config.ListenBrainzUrl(), _httpClientFactory, mbClient, logger, new SleepService());
    }
}
