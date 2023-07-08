using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Clients;
using Jellyfin.Plugin.Listenbrainz.Clients.ListenBrainz;
using Jellyfin.Plugin.Listenbrainz.Clients.MusicBrainz;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Models;
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
    private readonly ILogger<ResubmitListensTask> _logger;
    private readonly ListenBrainzClient _lbClient;
    private readonly IListenCache _listenCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResubmitListensTask"/> class.
    /// </summary>
    /// <param name="httpClientFactory">HTTP client factory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public ResubmitListensTask(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _logger = loggerFactory.CreateLogger<ResubmitListensTask>();

        var config = Plugin.GetConfiguration();
        _lbClient = new ListenBrainzClient(
            config.ListenBrainzUrl,
            _httpClientFactory,
            GetMusicBrainzClient(loggerFactory),
            loggerFactory.CreateLogger<ListenBrainzClient>(),
            new SleepService());

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
        await _listenCache.LoadFromFile();

        try
        {
            foreach (var user in config.LbUsers)
            {
                if (_listenCache.Get(user).Any())
                {
                    _logger.LogInformation("Found listens in cache for user {Username}, will try resubmitting", user.Name);
                    await SubmitListens(user, cancellationToken);
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

    private IMusicBrainzClient GetMusicBrainzClient(ILoggerFactory loggerFactory)
    {
        var config = Plugin.GetConfiguration();
        if (!config.GlobalConfig.MusicbrainzEnabled)
        {
            return new DummyMusicBrainzClient(loggerFactory.CreateLogger<DummyMusicBrainzClient>());
        }

        var logger = loggerFactory.CreateLogger<DefaultMusicBrainzClient>();
        return new DefaultMusicBrainzClient(config.MusicBrainzUrl, _httpClientFactory, logger, new SleepService());
    }

    private async Task SubmitListens(LbUser user, CancellationToken token)
    {
        var listenChunks = _listenCache.Get(user).Chunk(Limits.MaxListensPerRequest);
        foreach (var chunk in listenChunks)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                _lbClient.SubmitListens(user, chunk);
                _listenCache.Remove(user, chunk);
                await _listenCache.SaveToFile();
            }
            catch (ListenSubmitException)
            {
                _logger.LogInformation("Failed to resubmit listens for user {User}", user.Name);
                break;
            }
        }
    }
}
