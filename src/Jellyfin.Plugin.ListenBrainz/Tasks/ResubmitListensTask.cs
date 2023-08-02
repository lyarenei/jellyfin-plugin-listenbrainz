using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Managers;
using Jellyfin.Plugin.ListenBrainz.Utils;
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
    private readonly CacheManager _cacheManager;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly IMetadataClient _metadataClient;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResubmitListensTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    public ResubmitListensTask(ILoggerFactory loggerFactory, IHttpClientFactory clientFactory, IUserManager userManager, ILibraryManager libraryManager)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.ResubmitListensTask");
        _cacheManager = CacheManager.Instance;
        _listenBrainzClient = ClientUtils.GetListenBrainzClient(_logger, clientFactory, libraryManager);
        _metadataClient = ClientUtils.GetMusicBrainzClient(_logger, clientFactory);
        _userManager = userManager;
        _libraryManager = libraryManager;
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
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var config = Plugin.GetConfiguration();
        _cacheManager.Restore();

        try
        {
            foreach (var userConfig in config.UserConfigs)
            {
                if (_cacheManager.GetListens(userConfig.JellyfinUserId).Any())
                {
                    _logger.LogInformation(
                        "Found listens in cache for user {UserId}, will try resubmitting",
                        userConfig.JellyfinUserId);
                    cancellationToken.ThrowIfCancellationRequested();
                    SubmitListensForUser(userConfig.JellyfinUserId);
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

        return Task.CompletedTask;
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

    private void SubmitListensForUser(Guid userId)
    {
        var user = _userManager.GetUserById(userId);
        if (user is null) throw new ListenBrainzPluginException("Invalid jellyfin user ID");
        var listenChunks = _cacheManager.GetListens(userId).Chunk(Limits.MaxListensPerRequest);
        foreach (var listenChunk in listenChunks)
        {
            var chunkToSubmit = listenChunk.Select(UpdateMetadataIfNecessary);
            var userConfig = user.GetListenBrainzConfig();
            if (userConfig is null) throw new ListenBrainzPluginException($"No configuration for user {user.Username}");

            try
            {
                _listenBrainzClient.SendListens(userConfig, chunkToSubmit);
                _cacheManager.RemoveListens(userId, listenChunk);
                _cacheManager.Save();
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Failed to resubmit listens for user {User}: {Reason}", userId, ex.Message);
                _logger.LogDebug(ex, "Listen resubmit failed");
                break;
            }
        }
    }

    private StoredListen UpdateMetadataIfNecessary(StoredListen listen)
    {
        if (listen.Metadata is not null && !string.IsNullOrEmpty(listen.Metadata.RecordingMbid)) return listen;
        try
        {
            if (_libraryManager.GetItemById(listen.Id) is not Audio item) return listen;
            var metadata = _metadataClient.GetAudioItemMetadata(item).Result;
            listen.Metadata = metadata;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "No additional metadata available");
            _logger.LogInformation("No additional metadata available: {Reason}", e.Message);
        }

        return listen;
    }
}
