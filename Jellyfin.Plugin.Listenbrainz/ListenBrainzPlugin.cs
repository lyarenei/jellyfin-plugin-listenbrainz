using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.Listenbrainz.Clients.ListenBrainz;
using Jellyfin.Plugin.Listenbrainz.Exceptions;
using Jellyfin.Plugin.Listenbrainz.Extensions;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Resources.ListenBrainz;
using Jellyfin.Plugin.Listenbrainz.Services.ListenCache;
using Jellyfin.Plugin.Listenbrainz.Services.PlaybackTracker;
using Jellyfin.Plugin.Listenbrainz.Utils;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Listenbrainz;

/// <summary>
/// ListenBrainz implementation of playback tracker plugin.
/// </summary>
public class ListenBrainzPlugin : IPlaybackTrackerPlugin
{
    private readonly ILogger<ListenBrainzPlugin> _logger;
    private readonly IUserManager _userManager;
    private readonly ListenBrainzClient _lbClient;
    private readonly IPlaybackTrackerService _tracker;
    private readonly IListenCache _cache;

    // Lock for detecting duplicate data saved events
    private static readonly object _dataSavedLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzPlugin"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="userManager">User manager instance.</param>
    /// <param name="lbClient">ListenBrainz client.</param>
    /// <param name="tracker">Playback tracker.</param>
    /// <param name="cache">Listen cache.</param>
    public ListenBrainzPlugin(
        ILogger<ListenBrainzPlugin> logger,
        IUserManager userManager,
        ListenBrainzClient lbClient,
        IPlaybackTrackerService tracker,
        IListenCache cache)
    {
        _logger = logger;
        _userManager = userManager;
        _lbClient = lbClient;
        _tracker = tracker;
        _cache = cache;
    }

    /// <inheritdoc />
    public void OnPlaybackStarted(object? sender, PlaybackProgressEventArgs args)
    {
        EventData data;
        try
        {
            data = AssertPrerequisites(args);
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case ArgumentException:
                    _logger.LogDebug("Ignoring event: {Reason}", ex.Message);
                    break;
                case PluginConfigurationException:
                    _logger.LogInformation("Ignoring event: {Reason}", ex.Message);
                    break;
                default:
                    _logger.LogError("Ignoring event: {Reason}", ex.Message);
                    break;
            }

            return;
        }

        try
        {
            data.ListenBrainzUser.AssertCanSubmitListen();
        }
        catch (PluginConfigurationException ex)
        {
            _logger.LogInformation(
                "Ignoring event for user {Username}: {Reason}",
                data.JellyfinUser.Username,
                ex.Message);
            return;
        }

        var config = Plugin.GetConfiguration().GlobalConfig;
        if (config.AlternativeListenDetectionEnabled)
        {
            _logger.LogDebug(
                "Started playback tracking of item {ItemName} ({ItemId}) for user {Username}",
                data.AudioItem.Name,
                data.AudioItem.Id,
                data.JellyfinUser.Username);

            _tracker.StartTracking(data.AudioItem, data.JellyfinUser);
        }

        try
        {
            var listen = new Listen(data.AudioItem);
            _lbClient.NowPlaying(data.ListenBrainzUser, listen);
        }
        catch (Exception e)
        {
            _logger.LogWarning(
                "Failed to submit 'now playing' listen for user {User}: {Reason}",
                data.ListenBrainzUser.Name,
                e.Message);
        }
    }

    /// <inheritdoc />
    public async void OnPlaybackStopped(object? sender, PlaybackStopEventArgs args)
    {
        EventData data;
        try
        {
            data = AssertPrerequisites(args);
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case ArgumentException:
                    _logger.LogDebug("Ignoring event: {Reason}", ex.Message);
                    break;
                case PluginConfigurationException:
                    _logger.LogInformation("Ignoring event: {Reason}", ex.Message);
                    break;
                default:
                    _logger.LogError("Ignoring event: {Reason}", ex.Message);
                    break;
            }

            return;
        }

        try
        {
            Limits.EvaluateSubmitConditions(args.PlaybackPositionTicks, data.AudioItem.RunTimeTicks);
        }
        catch (Exception ex) when (ex is ArgumentNullException or ListenBrainzConditionsException)
        {
            _logger.LogInformation(
                "Listen won't be submitted for user {User}: {Reason}",
                data.ListenBrainzUser.Name,
                ex.Message);
            return;
        }

        var now = Helpers.GetCurrentTimestamp();
        var listen = new Listen(data.AudioItem, now);
        await HandleSubmitListen(data.ListenBrainzUser, listen);

        if (!data.ListenBrainzUser.Options.SyncFavoritesEnabled) return;

        var isFavorite = data.AudioItem.IsFavoriteOrLiked(data.JellyfinUser);
        await SyncFavorite(data.ListenBrainzUser, listen, isFavorite);
    }

    /// <inheritdoc />
    public async void OnUserDataSaved(object? sender, UserDataSaveEventArgs args)
    {
        EventData data;
        try
        {
            data = AssertPrerequisites(args);
        }
        catch (Exception ex)
        {
            switch (ex)
            {
                case ArgumentException:
                    _logger.LogDebug("Ignoring event: {Reason}", ex.Message);
                    break;
                case PluginConfigurationException:
                    _logger.LogInformation("Ignoring event: {Reason}", ex.Message);
                    break;
                default:
                    _logger.LogError("Ignoring event: {Reason}", ex.Message);
                    break;
            }

            return;
        }

        var trackedItem = _tracker.GetItem(data.AudioItem, data.JellyfinUser);
        if (trackedItem is not null)
        {
            lock (_dataSavedLock)
            {
                if (_tracker.GetItem(data.AudioItem, data.JellyfinUser) is null)
                {
                    // If we are here, then the item has been removed by earlier event and this one would be duplicate.
                    _logger.LogDebug(
                        "Detected duplicate playback report of {ItemName} ({ItemId}) for user {Username}, ignoring event",
                        data.AudioItem.Name,
                        data.AudioItem.Id,
                        data.JellyfinUser.Username);
                    return;
                }

                _logger.LogInformation(
                    "Found tracking of {ItemName} ({ItemId}) for user {Username}",
                    data.AudioItem.Name,
                    data.AudioItem.Id,
                    data.JellyfinUser.Username);

                var delta = DateTime.Now - trackedItem.StartedAt;
                var deltaTicks = delta.TotalSeconds * TimeSpan.TicksPerSecond;
                try
                {
                    Limits.EvaluateSubmitConditions((long)deltaTicks, data.AudioItem.RunTimeTicks);
                }
                catch (ListenBrainzConditionsException ex)
                {
                    _logger.LogInformation("Listen won't be submitted, conditions have not been met: {Reason}", ex.Message);
                    return;
                }

                _tracker.StopTracking(data.AudioItem, data.JellyfinUser);
            }
        }
        else
        {
            _logger.LogInformation(
                "No tracking of {ItemName} ({ItemId}) for user {Username}, assuming offline playback",
                data.AudioItem.Name,
                data.AudioItem.Id,
                data.JellyfinUser.Username);
        }

        var listenedAt = Helpers.TimestampFromDatetime(args.UserData.LastPlayedDate ?? DateTime.Now);
        var listen = new Listen(data.AudioItem, listenedAt);
        await HandleSubmitListen(data.ListenBrainzUser, listen);

        if (!data.ListenBrainzUser.Options.SyncFavoritesEnabled) return;

        var isFavorite = data.AudioItem.IsFavoriteOrLiked(data.JellyfinUser);
        await SyncFavorite(data.ListenBrainzUser, listen, isFavorite);
    }

    private async Task HandleSubmitListen(LbUser user, Listen listen)
    {
        try
        {
            _lbClient.SubmitListen(user, ListenType.Single, listen);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Listen submission for user {User} failed, adding to cache", user.Name);
            if (ex is ListenSubmitException exc && exc.Listen is not null)
            {
                _cache.Add(user, exc.Listen);
            }
            else
            {
                _cache.Add(user, listen);
            }

            await _cache.SaveToFile();
        }
    }

    private async Task SyncFavorite(LbUser user, Listen listen, bool isFavorite)
    {
        if (listen.RecordingMBID is not null)
        {
            _logger.LogDebug("Will use recording MBID for feedback submission");
            _lbClient.SubmitFeedback(user, listen, isFavorite);
            return;
        }

        _logger.LogDebug("Recording MBID is not available, will submit feedback using MSID");

        const int Retries = 7;
        const int BackOff = 3;
        var waitTime = 1;
        for (int i = 1; i <= Retries; i++)
        {
            var listenMsid = await _lbClient.GetMsidForListen(user, listen);
            if (listenMsid is not null)
            {
                _logger.LogDebug(
                    "Found recording MSID for listen of {ItemName} at {Timestamp} (user {Username}): {Msid}",
                    listen.Data.TrackName,
                    listen.ListenedAt,
                    user.Name,
                    listenMsid);

                listen.RecordingMsid = listenMsid;
                break;
            }

            _logger.LogDebug(
                "Could not find recording MSID for listen of {ItemName} at {Timestamp} (user {Username})",
                listen.Data.TrackName,
                listen.ListenedAt,
                user.Name);

            if (i + 1 > Retries)
            {
                _logger.LogInformation("Favorite sync failed; retry limit reached");
                return;
            }

            waitTime *= BackOff;
            _logger.LogDebug("Waiting {Seconds}s before trying again...", waitTime);
            Thread.Sleep(waitTime * 1000);
        }

        _lbClient.SubmitFeedback(user, listen, isFavorite);
    }

    private static EventData AssertPrerequisites(PlaybackProgressEventArgs args)
    {
        if (args.Item is not Audio item) throw new ArgumentException("Item in this event is not an audio");
        if (!item.HasRequiredMetadata())
        {
            // TODO:
            // throw new ItemMetadataException("Missing metadata: artist or track name");
            throw new Exception("Invalid metadata");
        }

        var jellyfinUser = args.Users.FirstOrDefault();
        if (jellyfinUser is null) throw new ArgumentException("This event does not have a Jellyfin user");

        var user = UserHelpers.GetListenBrainzUser(jellyfinUser);
        if (user is null) throw new PluginConfigurationException($"No configuration for user {jellyfinUser.Username}");

        user.AssertCanSubmitListen();
        return new EventData
        {
            ListenBrainzUser = user,
            JellyfinUser = jellyfinUser,
            AudioItem = item
        };
    }

    private EventData AssertPrerequisites(UserDataSaveEventArgs args)
    {
        if (args.Item is not Audio item) throw new ArgumentException("Item in this event is not an audio");
        if (args.SaveReason != UserDataSaveReason.PlaybackFinished)
        {
            throw new ArgumentException("Not a playback finished event");
        }

        if (!item.HasRequiredMetadata())
        {
            // TODO:
            // throw new ItemMetadataException("Missing metadata: artist or track name");
            throw new Exception("Invalid metadata");
        }

        var jellyfinUser = _userManager.GetUserById(args.UserId);
        if (jellyfinUser is null) throw new ArgumentException("This event does not have a Jellyfin user");

        var user = UserHelpers.GetListenBrainzUser(jellyfinUser);
        if (user is null) throw new PluginConfigurationException($"No configuration for user {jellyfinUser.Username}");

        user.AssertCanSubmitListen();
        return new EventData
        {
            ListenBrainzUser = user,
            JellyfinUser = jellyfinUser,
            AudioItem = item
        };
    }

    private struct EventData
    {
        public LbUser ListenBrainzUser { get; init; }

        public User JellyfinUser { get; init; }

        public Audio AudioItem { get; init; }
    }
}
