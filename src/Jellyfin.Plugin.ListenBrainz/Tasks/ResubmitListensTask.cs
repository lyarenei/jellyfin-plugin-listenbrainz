using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Managers;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using ClientUtils = Jellyfin.Plugin.ListenBrainz.Clients.Utils;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Jellyfin scheduled task for resubmitting listens.
/// </summary>
public class ResubmitListensTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IListensCacheManager _listensCache;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly IMusicBrainzClient _musicBrainzClient;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResubmitListensTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="listensCacheManager">Listens cache instance.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="musicBrainzClient">MusicBRainz client.</param>
    public ResubmitListensTask(
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        IUserManager userManager,
        ILibraryManager libraryManager,
        IListensCacheManager? listensCacheManager = null,
        IListenBrainzClient? listenBrainzClient = null,
        IMusicBrainzClient? musicBrainzClient = null)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.ResubmitListensTask");
        _userManager = userManager;
        _libraryManager = libraryManager;
        _listensCache = listensCacheManager ?? ListensCacheManager.Instance;
        _listenBrainzClient = listenBrainzClient ??
                              ClientUtils.GetListenBrainzClient(_logger, clientFactory, libraryManager);
        _musicBrainzClient = musicBrainzClient ?? ClientUtils.GetMusicBrainzClient(_logger, clientFactory);
    }

    /// <inheritdoc />
    public string Name => "Resubmit listens";

    /// <inheritdoc />
    public string Key => "ResubmitListens";

    /// <inheritdoc />
    public string Description => "Attempt to resubmit listens in cache to ListenBrainz.";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.GetConfiguration();
        await _listensCache.RestoreAsync();

        try
        {
            foreach (var userConfig in config.UserConfigs)
            {
                if (_listensCache.GetListens(userConfig.JellyfinUserId).Any())
                {
                    _logger.LogInformation(
                        "Found listens in cache for user {UserId}, will try resubmitting",
                        userConfig.JellyfinUserId);
                    cancellationToken.ThrowIfCancellationRequested();
                    await ProcessSavedListensForUser(config, userConfig, cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "User {UserId} does not have any cached listens, skipping",
                        userConfig.JellyfinUserId);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Listen resubmitting has been cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Listen resubmitting failed: {Reason}", ex.Message);
            _logger.LogDebug(ex, "Listen resubmitting failed");
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

    internal static long GetInterval()
    {
        var random = new Random();
        var randomMinute = random.Next(50);
        return TimeSpan.TicksPerDay + (randomMinute * TimeSpan.TicksPerMinute);
    }

    private async Task ProcessSavedListensForUser(
        PluginConfiguration pluginConfig,
        UserConfig userConfig,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var listenChunks = _listensCache.GetListens(userConfig.JellyfinUserId).Chunk(Limits.MaxListensPerRequest);
        foreach (var listenChunk in listenChunks)
        {
            var validListens = listenChunk
                .TakeWhile(IsValidListen)
                .Select(l => pluginConfig.IsMusicBrainzEnabled ? UpdateMetadataIfNecessary(l) : l)
                .WhereNotNull()
                .ToArray();

            await ProcessChunkOfStoredListens(validListens, userConfig, ct);
        }
    }

    internal async Task ProcessChunkOfStoredListens(
        StoredListen[] validListens,
        UserConfig userConfig,
        CancellationToken ct)
    {
        try
        {
            var listensToRemove = new List<StoredListen>();
            var listensToSend = validListens.Select(l =>
                {
                    var listen = _libraryManager.ToListen(l);
                    if (listen is null)
                    {
                        return null;
                    }

                    listensToRemove.Add(l);
                    return listen;
                })
                .WhereNotNull();
            await _listenBrainzClient.SendListensAsync(userConfig, listensToSend, ct);
            await _listensCache.RemoveListensAsync(userConfig.JellyfinUserId, listensToRemove);
            await _listensCache.SaveAsync();
        }
        catch (Exception ex)
        {
            _logger.LogInformation(
                "Failed to resubmit listens for user {User}: {Reason}",
                userConfig.JellyfinUserId,
                ex.Message);
            _logger.LogDebug(ex, "Listen resubmit failed");
        }
    }

    internal bool IsValidListen(StoredListen listen)
    {
        try
        {
            return _libraryManager.ToListen(listen) is not null;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Failed to prepare cached listen for submission: {Reason}", ex.Message);
            _logger.LogDebug(ex, "Recreating listen from cache failed");
            return false;
        }
    }

    internal StoredListen? UpdateMetadataIfNecessary(StoredListen listen)
    {
        if (_libraryManager.GetItemById(listen.Id) is not Audio item)
        {
            _logger.LogWarning("Item with ID {ListenID} is not an audio item", listen.Id);
            return null;
        }

        if (listen.HasRecordingMbid)
        {
            return listen;
        }

        try
        {
            listen.Metadata = _musicBrainzClient.GetAudioItemMetadata(item);
        }
        catch (Exception e)
        {
            _logger.LogInformation("No additional metadata available: {Reason}", e.Message);
            _logger.LogDebug(e, "No additional metadata available");
        }

        return listen;
    }
}
