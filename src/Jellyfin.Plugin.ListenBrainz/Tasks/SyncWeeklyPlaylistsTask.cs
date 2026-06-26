using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using JellyfinPlaylist = MediaBrowser.Controller.Playlists.Playlist;
using Playlist = Jellyfin.Plugin.ListenBrainz.Api.Models.Playlist;
using Utils = Jellyfin.Plugin.ListenBrainz.Common.Utils;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Jellyfin task for syncing weekly rotation playlists from ListenBrainz.
/// </summary>
public class SyncWeeklyPlaylistsTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IListenBrainzService _listenBrainz;
    private readonly IUserManager _userManager;
    private readonly IPluginConfigService _configService;
    private readonly IPlaylistSyncStateService _stateService;
    private readonly IPlaylistTrackMatcher _trackMatcher;
    private readonly IPlaylistManager _playlistManager;
    private double _progress;
    private double _userCountRatio;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncWeeklyPlaylistsTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="listenBrainz">ListenBrainz service.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="stateService">Playlist sync state service.</param>
    /// <param name="trackMatcher">Playlist track matcher.</param>
    /// <param name="playlistManager">Weekly playlist writer.</param>
    public SyncWeeklyPlaylistsTask(
        ILoggerFactory loggerFactory,
        IUserManager userManager,
        IListenBrainzService listenBrainz,
        IPluginConfigService configService,
        IPlaylistSyncStateService stateService,
        IPlaylistTrackMatcher trackMatcher,
        IPlaylistManager playlistManager)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.SyncWeeklyPlaylistsTask");
        _listenBrainz = listenBrainz;
        _userManager = userManager;
        _configService = configService;
        _stateService = stateService;
        _trackMatcher = trackMatcher;
        _playlistManager = playlistManager;
    }

    /// <inheritdoc />
    public string Name => "Sync weekly playlists from ListenBrainz";

    /// <inheritdoc />
    public string Key => "SyncWeeklyPlaylists";

    /// <inheritdoc />
    public string Description => "Sync weekly ListenBrainz rotation playlists to Jellyfin";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() =>
    [
        new()
        {
            Type = TaskTriggerInfoType.WeeklyTrigger,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDayTicks = Utils.GetRandomMinute() * TimeSpan.TicksPerMinute,
        },
    ];

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        using var logScope = BeginLogScope();
        var enabledUserConfigs = _configService
            .UserConfigs
            .Where(uc => uc.IsWeeklyPlaylistsSyncEnabled)
            .ToList();

        if (enabledUserConfigs.Count == 0)
        {
            _logger.LogInformation("No users have weekly playlist syncing enabled, nothing to sync");
            progress.Report(100);
            return;
        }

        _logger.LogInformation("Starting weekly playlist sync from ListenBrainz...");
        ResetProgress(enabledUserConfigs.Count);

        var state = await _stateService.ReadAsync(cancellationToken);
        try
        {
            foreach (var userConfig in enabledUserConfigs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Syncing weekly playlists for user {Username}", userConfig.UserName);
                await HandleUserPlaylistSync(progress, userConfig, state, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Weekly playlist sync task has been cancelled");
            progress.Report(100);
        }
        finally
        {
            await _stateService.SaveAsync(state, CancellationToken.None);
        }
    }

    private async Task HandleUserPlaylistSync(
        IProgress<double> progress,
        UserConfig userConfig,
        PlaylistSyncState state,
        CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (user is null)
        {
            _logger.LogWarning("User with ID {UserId} does not exist", userConfig.JellyfinUserId);
            ReportUserDone(progress);
            return;
        }

        try
        {
            var playlists = (await _listenBrainz.GetCreatedForPlaylistsAsync(
                userConfig,
                Limits.MaxItemsPerGet,
                cancellationToken)).ToList();

            _logger.LogInformation(
                "Found {Count} playlists created for user {Username}",
                playlists.Count,
                userConfig.UserName);

            var weeklyPlaylists = WeeklyRotationPolicy.PickWeeklyRotationPlaylists(playlists, userConfig).ToList();
            _logger.LogInformation(
                "Selected {Count} weekly rotation playlists for user {Username}",
                weeklyPlaylists.Count,
                userConfig.UserName);

            PruneOutOfRotationPlaylists(user, userConfig, state, weeklyPlaylists, cancellationToken);

            if (weeklyPlaylists.Count == 0)
            {
                ReportUserDone(progress);
                return;
            }

            var playlistRatio = _userCountRatio / weeklyPlaylists.Count;
            var candidates = _trackMatcher.GetCandidateAudioItems(user);
            foreach (var weeklyPlaylist in weeklyPlaylists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogDebug(
                        "Processing weekly playlist {PlaylistId} of type {PlaylistType}",
                        weeklyPlaylist.Playlist.PlaylistId,
                        weeklyPlaylist.Type);

                    var playlist = await _listenBrainz.GetPlaylistAsync(
                        userConfig,
                        weeklyPlaylist.Playlist.PlaylistId,
                        cancellationToken);

                    await SyncPlaylist(user, playlist, weeklyPlaylist.Type, candidates, state, cancellationToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogWarning(
                        "Failed to sync weekly playlist {PlaylistId}: {Error}",
                        weeklyPlaylist.Playlist.PlaylistId,
                        e.Message);
                }

                _progress += playlistRatio;
                progress.Report(_progress);
            }
        }
        catch (PluginException e)
        {
            _logger.LogError(
                "Failed to fetch weekly playlists for user {Username}: {Error}",
                userConfig.UserName,
                e.Message);
            ReportUserDone(progress);
        }
    }

    private async Task SyncPlaylist(
        User user,
        Playlist playlist,
        WeeklyPlaylistType playlistType,
        IReadOnlyList<BaseItem> candidates,
        PlaylistSyncState state,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Syncing weekly playlist: {Title}", playlist.Title);

        var tracks = playlist.Tracks.ToList();
        if (tracks.Count == 0)
        {
            _logger.LogDebug("Playlist {Title} has no tracks, skipping", playlist.Title);
            return;
        }

        var matchedTracks = new List<BaseItem>();
        foreach (var track in tracks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = await _trackMatcher.FindMatchAsync(candidates, user, track, cancellationToken);
            if (item is null)
            {
                _logger.LogDebug("No Jellyfin item found for track: {Title}", track.Title);
                continue;
            }

            matchedTracks.Add(item);
        }

        _logger.LogInformation(
            "Found {Count} (out of {TotalCount}) matching tracks for weekly playlist {Title}",
            matchedTracks.Count,
            tracks.Count,
            playlist.Title);

        var existingPlaylist = ResolveMappedPlaylist(user, state, playlist.PlaylistId) ??
                               _playlistManager.FindByName(user, playlist.Title);

        Guid jellyfinPlaylistId;
        if (existingPlaylist is null)
        {
            jellyfinPlaylistId = await _playlistManager.CreateAsync(
                user,
                playlist.Title,
                matchedTracks,
                cancellationToken);
        }
        else
        {
            jellyfinPlaylistId = existingPlaylist.Id;
            await _playlistManager.ReplaceTracksAsync(user, existingPlaylist, matchedTracks, cancellationToken);
        }

        state.Upsert(
            user.Id,
            playlist.PlaylistId,
            jellyfinPlaylistId,
            playlist.Title,
            playlist.CreatedAt,
            WeeklyRotationPolicy.CategoryFor(playlistType));

        _logger.LogInformation(
            "Successfully synced weekly playlist {Name} with {Count} tracks",
            playlist.Title,
            matchedTracks.Count);
    }

    private JellyfinPlaylist? ResolveMappedPlaylist(User user, PlaylistSyncState state, string listenBrainzPlaylistId)
    {
        var mapping = state.FindMapping(user.Id, listenBrainzPlaylistId);
        if (mapping is null)
        {
            return null;
        }

        var playlist = _playlistManager.Find(mapping.JellyfinPlaylistId, user.Id);
        if (playlist is not null)
        {
            return playlist;
        }

        _logger.LogInformation(
            "Mapped Jellyfin playlist {PlaylistId} for ListenBrainz playlist {ListenBrainzPlaylistId} no longer exists",
            mapping.JellyfinPlaylistId,
            mapping.ListenBrainzPlaylistId);
        state.Mappings.Remove(mapping);
        return null;
    }

    private void PruneOutOfRotationPlaylists(
        User user,
        UserConfig userConfig,
        PlaylistSyncState state,
        IReadOnlyList<WeeklyPlaylistCandidate> rotationPlaylists,
        CancellationToken cancellationToken)
    {
        if (userConfig.KeepWeeklyPlaylistsAfterRotation)
        {
            return;
        }

        var rotationIds = rotationPlaylists
            .Select(p => p.Playlist.PlaylistId)
            .WhereNotNull()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var rotationTypes = rotationPlaylists.Select(p => p.Type).ToHashSet();

        var mappingsToRemove = state
            .Mappings
            .Where(m => m.JellyfinUserId == user.Id &&
                        WeeklyRotationPolicy.ShouldPruneMapping(userConfig, m, rotationIds, rotationTypes))
            .ToList();

        foreach (var mapping in mappingsToRemove)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var playlist = _playlistManager.Find(mapping.JellyfinPlaylistId, user.Id);
            if (playlist is not null)
            {
                _logger.LogInformation(
                    "Deleting weekly playlist {PlaylistName} because it is no longer in rotation",
                    playlist.Name);
                _playlistManager.Delete(playlist);
            }

            state.Mappings.Remove(mapping);
        }
    }

    private void ResetProgress(int userCount)
    {
        _userCountRatio = 100.0 / userCount;
        _progress = 0;
    }

    private void ReportUserDone(IProgress<double> progress)
    {
        _progress += _userCountRatio;
        progress.Report(_progress);
    }

    private IDisposable? BeginLogScope()
    {
        return _logger.BeginScope(new Dictionary<string, object> { { "EventId", "SyncWeeklyPlaylistsTask" } });
    }
}
