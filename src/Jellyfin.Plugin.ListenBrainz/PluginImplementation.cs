using Jellyfin.Data.Entities;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Managers;
using Jellyfin.Plugin.ListenBrainz.Utils;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz;

/// <summary>
/// ListenBrainz plugin implementation.
/// </summary>
public class PluginImplementation
{
    private readonly ILogger _logger;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly IMetadataClient _metadataClient;
    private readonly IUserDataManager _userDataManager;
    private readonly ListensCacheManager _listensCache;
    private readonly IUserManager _userManager;
    private readonly object _userDataSaveLock = new();
    private readonly PlaybackTrackingManager _playbackTracker;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginImplementation"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="metadataClient">Client for providing additional metadata.</param>
    /// <param name="userDataManager">User data manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    public PluginImplementation(
        ILogger logger,
        IListenBrainzClient listenBrainzClient,
        IMetadataClient metadataClient,
        IUserDataManager userDataManager,
        IUserManager userManager,
        ILibraryManager libraryManager)
    {
        _logger = logger;
        _listenBrainzClient = listenBrainzClient;
        _metadataClient = metadataClient;
        _userDataManager = userDataManager;
        _listensCache = ListensCacheManager.Instance;
        _userManager = userManager;
        _playbackTracker = PlaybackTrackingManager.Instance;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Sends 'playing now' listen to ListenBrainz.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStart(object? sender, PlaybackProgressEventArgs args)
    {
        // TODO: event ID logger scope
        _logger.LogDebug("Picking up playback start event for item {Item}", args.Item.Name);
        EventData data;
        try
        {
            data = GetEventData(args);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Dropping event");
            return;
        }

        _logger.LogInformation(
            "Processing playback start event for item {Item} associated with user {Username}",
            data.Item.Name,
            data.JellyfinUser.Username);

        UserConfig userConfig;
        try
        {
            AssertInAllowedLibrary(data.Item);
            userConfig = data.JellyfinUser.GetListenBrainzConfig();
            AssertSubmissionEnabled(userConfig);
            AssertBasicMetadataRequirements(data.Item);
        }
        catch (Exception e)
        {
            _logger.LogInformation("Dropping event: {Reason}", e.Message);
            _logger.LogDebug(e, "Dropping event");
            return;
        }

        _logger.LogDebug("All checks passed, preparing for sending listen");

        AudioItemMetadata? metadata = null;
        try
        {
            AssertMusicBrainzIsEnabled();
            _logger.LogInformation("Getting additional metadata...");
            metadata = _metadataClient.GetAudioItemMetadata(data.Item);
            _logger.LogInformation("Additional metadata successfully received");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Additional metadata are not available: {Reason}", e.Message);
            _logger.LogDebug(e, "Additional metadata are not available");
        }

        try
        {
            _logger.LogInformation("Sending 'playing now' listen...");
            _listenBrainzClient.SendNowPlaying(userConfig, data.Item, metadata);
            _logger.LogInformation("Successfully sent 'playing now' listen");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Failed to send 'playing now' listen: {Reason}", e.Message);
            _logger.LogDebug(e, "Failed to send 'playing now' listen");
        }

        if (Plugin.GetConfiguration().IsAlternativeModeEnabled)
        {
            _logger.LogDebug("Alternative mode is enabled, adding item to playback tracker");
            _playbackTracker.AddItem(data.JellyfinUser.Id.ToString(), data.Item);
        }

        _logger.LogDebug("Event has been successfully processed");
    }

    /// <summary>
    /// Sends listen of track to ListenBrainz if conditions have been met.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnPlaybackStop(object? sender, PlaybackStopEventArgs args)
    {
        // TODO: event ID logger scope
        _logger.LogDebug("Picking up playback stop event for item {Item}", args.Item.Name);
        var config = Plugin.GetConfiguration();
        if (config.IsAlternativeModeEnabled)
        {
            _logger.LogDebug("Dropping event - alternative mode is enabled");
            return;
        }

        EventData data;
        try
        {
            data = GetEventData(args);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Dropping event");
            return;
        }

        _logger.LogInformation(
            "Processing playback stop event for item {Item} associated with user {Username}",
            data.Item.Name,
            data.JellyfinUser.Username);

        UserConfig userConfig;
        try
        {
            AssertInAllowedLibrary(data.Item);
            userConfig = data.JellyfinUser.GetListenBrainzConfig();
            AssertSubmissionEnabled(userConfig);
            AssertBasicMetadataRequirements(data.Item);
            AssertListenBrainzRequirements(args, data);
        }
        catch (Exception e)
        {
            _logger.LogInformation("Dropping event: {Reason}", e.Message);
            _logger.LogDebug(e, "Dropping event");
            return;
        }

        _logger.LogDebug("All checks passed, preparing for sending listen");
        var now = DateUtils.CurrentTimestamp;

        AudioItemMetadata? metadata = null;
        try
        {
            AssertMusicBrainzIsEnabled();
            _logger.LogInformation("Getting additional metadata...");
            metadata = _metadataClient.GetAudioItemMetadata(data.Item);
            _logger.LogInformation("Additional metadata successfully received");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Additional metadata are not available: {Reason}", e.Message);
            _logger.LogDebug(e, "Additional metadata are not available");
        }

        try
        {
            _logger.LogInformation("Sending listen...");
            _listenBrainzClient.SendNowPlaying(userConfig, data.Item, metadata);
            _logger.LogInformation("Listen successfully sent");
        }
        catch (Exception e)
        {
            _logger.LogInformation("Failed to send listen: {Reason}", e.Message);
            _logger.LogDebug(e, "Failed to send listen");

            _listensCache.AddListen(data.JellyfinUser.Id, data.Item, metadata, now);
            _listensCache.Save();
            _logger.LogInformation("Listen has been added to the cache");
            return;
        }

        if (userConfig.IsFavoritesSyncEnabled)
        {
            HandleFavoriteSync(data, metadata, userConfig, now);
        }
    }

    /// <summary>
    /// Sends listen of track to ListenBrainz if conditions have been met.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="args">Event args.</param>
    public void OnUserDataSave(object? sender, UserDataSaveEventArgs args)
    {
        _logger.LogDebug("Picking up user data save event for item {Item}", args.Item.Name);
        var config = Plugin.GetConfiguration();
        if (!config.IsAlternativeModeEnabled)
        {
            _logger.LogDebug("Dropping event - alternative mode is disabled");
            return;
        }

        EventData data;
        try
        {
            data = GetEventData(args);
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Invalid event");
            return;
        }

        if (!IsInAllowedLibrary(data.Item))
        {
            _logger.LogInformation(
                "Dropping event for item {Item}: Item is in any allowed libraries",
                data.Item.Name);
            return;
        }

        var userConfig = data.JellyfinUser.GetListenBrainzConfig();
        if (userConfig is null)
        {
            _logger.LogWarning(
                "Dropping event for track {Track}: User {User} is not configured",
                data.Item.Name,
                data.JellyfinUser.Username);

            return;
        }

        if (userConfig.IsNotListenSubmitEnabled)
        {
            _logger.LogInformation(
                "Dropping event for track {Track}: User {User} does not have listen submitting enabled",
                data.Item.Name,
                data.JellyfinUser.Username);
            return;
        }

        try
        {
            Monitor.Enter(_userDataSaveLock);
            EvaluateConditionsIfTracked(data.Item, data.JellyfinUser);
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                "Dropping event for track {Track} and user {User}: {Reason}",
                data.Item.Name,
                data.JellyfinUser.Username,
                e.Message);

            _logger.LogDebug(e, "Event will not be handled");
            return;
        }
        finally
        {
            Monitor.Exit(_userDataSaveLock);
        }

        var now = DateUtils.CurrentTimestamp;
        var metadata = GetAdditionalMetadata(data);
        try
        {
            _listenBrainzClient.SendListen(userConfig, data.Item, metadata, now);
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                "Failed to send listen of {Track} for user {User}: {Reason}",
                data.Item.Name,
                data.JellyfinUser.Username,
                e.Message);

            _logger.LogDebug(e, "Send listen failed");
            _listensCache.AddListen(data.JellyfinUser.Id, data.Item, metadata, now);
            _listensCache.Save();
        }

        if (!userConfig.IsFavoritesSyncEnabled) return;
        HandleFavoriteSync(data, metadata, userConfig, now);
    }

    private async void HandleFavoriteSync(EventData data, AudioItemMetadata? metadata, UserConfig userConfig, long listenTs)
    {
        _logger.LogInformation(
            "Attempting to sync favorite status for track {Track} and user {User}",
            data.Item.Name,
            data.JellyfinUser.Username);

        try
        {
            var userItemData = _userDataManager.GetUserData(data.JellyfinUser, data.Item);
            if (metadata?.RecordingMbid is not null)
            {
                _listenBrainzClient.SendFeedback(userConfig, userItemData.IsFavorite, metadata.RecordingMbid);
                _logger.LogInformation("Favorite sync for track {Track} has been successful", data.Item.Name);
                return;
            }

            _logger.LogInformation("No MBID is available, will attempt to sync favorite status using MSID");
            await SendFeedbackUsingMsid(userConfig, userItemData.IsFavorite, listenTs);
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                "Favorite sync for track {Track} and user {User} failed: {Reason}",
                data.Item.Name,
                data.JellyfinUser.Username,
                e.Message);

            _logger.LogDebug(e, "Favorite sync failed");
        }
    }

    /// <summary>
    /// Assert MusicBrainz integration is enabled.
    /// </summary>
    /// <exception cref="PluginException">MusicBrainz integration is not enabled.</exception>
    private void AssertMusicBrainzIsEnabled()
    {
        _logger.LogDebug("Checking if MusicBrainz integration is enabled");
        if (!Plugin.GetConfiguration().IsMusicBrainzEnabled)
        {
            throw new PluginException("MusicBrainz integration is disabled");
        }

        _logger.LogDebug("MusicBrainz integration is enabled");
    }

    private AudioItemMetadata? GetAdditionalMetadata(EventData data)
    {
        if (!Plugin.GetConfiguration().IsMusicBrainzEnabled) return null;

        _logger.LogInformation(
            "MusicBrainz integration is enabled, fetching metadata for track {Track}",
            data.Item.Name);

        try
        {
            return _metadataClient.GetAudioItemMetadataAsync(data.Item).Result;
        }
        catch (Exception e)
        {
            _logger.LogInformation(
                "No additional metadata available for track {Track}: {Reason}",
                data.Item.Name,
                e.Message);

            _logger.LogDebug(e, "No additional metadata available");
        }

        return null;
    }

    /// <summary>
    /// Verifies the event.
    /// </summary>
    /// <param name="eventArgs">Event arguments.</param>
    /// <returns>Event data.</returns>
    /// <exception cref="ArgumentException">Event data are not valid.</exception>
    private static EventData GetEventData(PlaybackProgressEventArgs eventArgs)
    {
        if (eventArgs.Item is not Audio item)
        {
            throw new ArgumentException("Event item is not an Audio item");
        }

        var jellyfinUser = eventArgs.Users.FirstOrDefault();
        if (jellyfinUser is null)
        {
            throw new ArgumentException("No user is associated with this event");
        }

        return new EventData
        {
            Item = item,
            JellyfinUser = jellyfinUser
        };
    }

    private EventData GetEventData(UserDataSaveEventArgs eventArgs)
    {
        if (eventArgs.Item is not Audio item)
        {
            throw new ArgumentException("Event item is not an Audio item");
        }

        if (eventArgs.SaveReason is not UserDataSaveReason.PlaybackFinished)
        {
            throw new ArgumentException("Not a playback finished event");
        }

        var jellyfinUser = _userManager.GetUserById(eventArgs.UserId);
        if (jellyfinUser is null)
        {
            throw new ArgumentException("No user is associated with this event");
        }

        return new EventData
        {
            Item = item,
            JellyfinUser = jellyfinUser
        };
    }

    /// <summary>
    /// Check if the item has basic metadata.
    /// </summary>
    /// <param name="item">Item to be checked.</param>
    /// <exception cref="PluginException">Item does not meet requirements.</exception>
    private void AssertBasicMetadataRequirements(Audio item)
    {
        _logger.LogDebug("Checking item basic metadata");
        try
        {
            item.AssertHasMetadata();
        }
        catch (Exception e)
        {
            throw new PluginException("Item metadata are not valid", e);
        }

        _logger.LogDebug("Item has basic metadata");
    }

    /// <summary>
    /// Assert user has enabled listen submission.
    /// </summary>
    /// <param name="config">User configuration.</param>
    /// <exception cref="PluginException">User has disabled listen submission.</exception>
    private void AssertSubmissionEnabled(UserConfig config)
    {
        _logger.LogDebug("Checking if ListenBrainz submission is enabled for the user");
        if (config.IsNotListenSubmitEnabled)
        {
            throw new PluginException("ListenBrainz submission is disabled for the user");
        }

        _logger.LogDebug("ListenBrainz submission is enabled for the user");
    }

    private async Task<bool> SendFeedbackUsingMsid(UserConfig userConfig, bool isFavorite, long listenTs)
    {
        const int MaxAttempts = 4;
        const int BackOffSecs = 5;
        var sleepSecs = 1;

        // TODO: Improve logging

        // Delay to maximize the chance of getting it on first try
        await Task.Delay(500);
        for (int i = 0; i < MaxAttempts; i++)
        {
            var msid = await _listenBrainzClient.GetRecordingMsidByListenTs(userConfig, listenTs);
            if (msid is not null)
            {
                _listenBrainzClient.SendFeedback(userConfig, isFavorite, recordingMsid: msid);
                _logger.LogInformation("Favorite sync has been successful");
                return true;
            }

            sleepSecs *= BackOffSecs;
            sleepSecs += new Random().Next(20);
            _logger.LogDebug(
                "Recording MSID with listen timestamp {Ts} not found, will retry in {Secs} seconds",
                listenTs,
                sleepSecs);

            await Task.Delay(sleepSecs * 1000);
        }

        _logger.LogInformation("Favorite sync for track failed - maximum retry attempts have been reached");
        return false;
    }

    private void EvaluateConditionsIfTracked(BaseItem item, User user)
    {
        var trackedItem = _playbackTracker.GetItem(user.Id.ToString(), item.Id.ToString());
        if (trackedItem is null)
        {
            _logger.LogInformation(
                "No playback is tracked for user {User} and track {Track}, assuming offline playback",
                user.Username,
                item.Name);

            return;
        }

        if (!trackedItem.IsValid)
        {
            throw new PluginException("Playback tracking is not valid for this item");
        }

        var delta = DateUtils.CurrentTimestamp - trackedItem.StartedAt;
        var deltaTicks = delta * TimeSpan.TicksPerSecond;
        var runtime = item.RunTimeTicks ?? 0;
        Limits.AssertSubmitConditions(deltaTicks, runtime);
        _playbackTracker.InvalidateItem(user.Id.ToString(), trackedItem);
    }

    /// <summary>
    /// Verifies if the specified item is in any allowed library.
    /// </summary>
    /// <param name="item">Item to verify.</param>
    /// <exception cref="PluginException">Item is not in any allowed library.</exception>
    private void AssertInAllowedLibrary(BaseItem item)
    {
        _logger.LogDebug("Checking if item is in any allowed libraries");
        var isInAllowed = _libraryManager
            .GetCollectionFolders(item)
            .Select(il => il.Id)
            .Intersect(GetAllowedLibraries())
            .Any();

        if (!isInAllowed)
        {
            throw new PluginException("Item is not in any allowed library");
        }

        _logger.LogDebug("Item is in at least one allowed library");
    }

    private bool IsInAllowedLibrary(BaseItem item)
    {
        var itemLibraries = _libraryManager.GetCollectionFolders(item);
        return itemLibraries
            .Select(il => il.Id)
            .Intersect(GetAllowedLibraries())
            .Any();
    }

    private IEnumerable<Guid> GetAllowedLibraries()
    {
        var allLibraries = Plugin.GetConfiguration().LibraryConfigs;
        if (allLibraries.Any())
        {
            return allLibraries.Where(lc => lc.IsAllowed).Select(lc => lc.Id);
        }

        return _libraryManager.GetMusicLibraries().Select(ml => ml.Id);
    }

    private void AssertListenBrainzRequirements(PlaybackStopEventArgs args, EventData data)
    {
        _logger.LogDebug("Checking ListenBrainz requirements");
        var position = args.PlaybackPositionTicks;
        if (position is null)
        {
            throw new PluginException("Playback position is not set");
        }

        try
        {
            Limits.AssertSubmitConditions((long)position, data.Item.RunTimeTicks ?? 0);
        }
        catch (Exception e)
        {
            throw new PluginException("Requirements were not met", e);
        }

        _logger.LogDebug("Requirements were met");
    }

    private struct EventData
    {
        public Audio Item { get; init; }

        public User JellyfinUser { get; init; }
    }
}
