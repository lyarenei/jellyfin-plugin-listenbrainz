using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Jellyfin scheduled task for resubmitting listens.
/// </summary>
public class ResubmitListensTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IListensCachingService _listensCache;
    private readonly IListenBrainzService _listenBrainz;
    private readonly IMetadataProviderService _metadataProvider;
    private readonly IPluginConfigService _pluginConfig;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResubmitListensTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="serviceFactory">Service factory.</param>
    public ResubmitListensTask(
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        ILibraryManager libraryManager,
        IServiceFactory? serviceFactory = null)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.ResubmitListensTask");
        _libraryManager = libraryManager;

        var factory = serviceFactory ?? new DefaultServiceFactory(loggerFactory, clientFactory);
        _listenBrainz = factory.GetListenBrainzService();
        _metadataProvider = factory.GetMetadataProviderService();
        _pluginConfig = factory.GetPluginConfigService();
        _listensCache = factory.GetListensCachingService();
    }

    /// <inheritdoc />
    public string Name => "Resubmit listens";

    /// <inheritdoc />
    public string Key => "ResubmitListens";

    /// <inheritdoc />
    public string Description => "(Re)submit listens currently stored in a cache.";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var userConfig in _pluginConfig.UserConfigs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessSavedListensForUser(userConfig, cancellationToken);
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
        return
        [
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = GetInterval(),
            },
        ];
    }

    internal static long GetInterval()
    {
        var random = new Random();
        var randomMinute = random.Next(50);
        return TimeSpan.TicksPerDay + (randomMinute * TimeSpan.TicksPerMinute);
    }

    private async Task ProcessSavedListensForUser(UserConfig userConfig, CancellationToken ct)
    {
        _logger.LogInformation(
            "Processing cached listens for user {UserId} (associated with ListenBrainz user {UserName}",
            userConfig.JellyfinUserId,
            userConfig.UserName);

        var userListens = _listensCache.GetListens(userConfig.JellyfinUserId).ToList();
        if (userListens.Count < 1)
        {
            _logger.LogInformation("User {UserId} does not have any cached listens", userConfig.JellyfinUserId);
            return;
        }

        _logger.LogInformation(
            "Found {Num} listens in cache for user {UserId}, will try resubmitting",
            userListens.Count,
            userConfig.JellyfinUserId);

        var validListens = userListens
            .Where(IsValidListen)
            .WhereNotNull()
            .ToList();

        var listenChunks = validListens.Chunk(Limits.MaxListensPerRequest);
        foreach (var listenChunk in listenChunks)
        {
            await ProcessChunkOfListens(listenChunk, userConfig, ct);
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

    internal async Task ProcessChunkOfListens(StoredListen[] storedListens, UserConfig userConfig, CancellationToken cancellationToken)
    {
        var listensToRemove = new List<StoredListen>();
        var listensToSend = new List<Listen>();
        foreach (var storedListen in storedListens)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_pluginConfig.IsMusicBrainzEnabled && !storedListen.HasRecordingMbid)
            {
                storedListen.Metadata = await GetAudioItemMetadataAsync(storedListen, cancellationToken);
            }

            var listen = _libraryManager.ToListen(storedListen);
            if (listen is null)
            {
                _logger.LogDebug("Failed to recreate listen of item {ItemId}", storedListen.Id);
                continue;
            }

            listensToRemove.Add(storedListen);
            listensToSend.Add(listen);
        }

        if (listensToSend.Count < 1)
        {
            _logger.LogInformation("No listens to resubmit in the current chunk");
            return;
        }

        var isOk = await _listenBrainz.SendListensAsync(userConfig, listensToSend, cancellationToken);
        if (isOk)
        {
            _logger.LogInformation("Successfully resubmitted {Count} listen(s)", listensToSend.Count);
            await _listensCache.RemoveListensAsync(userConfig.JellyfinUserId, listensToRemove);
            await _listensCache.SaveAsync();
        }
        else
        {
            _logger.LogInformation("Failed to resubmit {Count} listen(s)", listensToSend.Count);
        }
    }

    internal async Task<AudioItemMetadata?> GetAudioItemMetadataAsync(
        StoredListen listen,
        CancellationToken cancellationToken)
    {
        if (_libraryManager.GetItemById(listen.Id) is not Audio item)
        {
            _logger.LogDebug("Item with ID {ListenID} is not an audio item", listen.Id);
            return null;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await _metadataProvider.GetAudioItemMetadataAsync(item, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Fetching additional metadata failed: {Reason}", ex.GetFullMessage());
            return null;
        }
    }
}
