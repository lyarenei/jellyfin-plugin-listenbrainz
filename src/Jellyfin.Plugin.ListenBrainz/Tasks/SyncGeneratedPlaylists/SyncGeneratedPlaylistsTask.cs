using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using JellyfinPlaylist = MediaBrowser.Controller.Playlists.Playlist;
using Playlist = Jellyfin.Plugin.ListenBrainz.Api.Models.Playlist;
using Utils = Jellyfin.Plugin.ListenBrainz.Common.Utils;

namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncGeneratedPlaylists;

/// <summary>
/// Jellyfin task for syncing generated playlists from ListenBrainz.
/// </summary>
/// <remarks>
/// The task runs in two stages. Discovery finds the playlists to sync, the sync stage then writes
/// only those which need writing.
/// </remarks>
public class SyncGeneratedPlaylistsTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IUserManager _userManager;
    private readonly IPluginConfigService _configService;
    private readonly IPlaylistDiscoveryService _discoveryService;
    private readonly IPlaylistSyncStateService _stateService;
    private readonly IListenBrainzService _listenBrainz;
    private readonly IPlaylistTrackMatcher _trackMatcher;
    private readonly IPlaylistManager _playlistManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncGeneratedPlaylistsTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="discoveryService">Playlist discovery service.</param>
    /// <param name="stateService">Playlist sync state service.</param>
    /// <param name="listenBrainz">ListenBrainz service.</param>
    /// <param name="trackMatcher">Playlist track matcher.</param>
    /// <param name="playlistManager">Playlist writer.</param>
    public SyncGeneratedPlaylistsTask(
        ILoggerFactory loggerFactory,
        IUserManager userManager,
        IPluginConfigService configService,
        IPlaylistDiscoveryService discoveryService,
        IPlaylistSyncStateService stateService,
        IListenBrainzService listenBrainz,
        IPlaylistTrackMatcher trackMatcher,
        IPlaylistManager playlistManager)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.SyncGeneratedPlaylistsTask");
        _userManager = userManager;
        _configService = configService;
        _discoveryService = discoveryService;
        _stateService = stateService;
        _listenBrainz = listenBrainz;
        _trackMatcher = trackMatcher;
        _playlistManager = playlistManager;
    }

    /// <inheritdoc />
    public string Name => "Sync generated playlists from ListenBrainz";

    /// <inheritdoc />
    public string Key => "SyncGeneratedPlaylists";

    /// <inheritdoc />
    public string Description => "Sync generated ListenBrainz playlists to Jellyfin";

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
            .Where(uc => uc.IsGeneratedPlaylistsSyncEnabled)
            .ToList();

        if (enabledUserConfigs.Count == 0)
        {
            _logger.LogInformation("No users have generated playlist syncing enabled, nothing to sync");
            progress.Report(100);
            return;
        }

        _logger.LogInformation("Starting generated playlist sync from ListenBrainz...");
        var reporter = new SyncProgress(progress, enabledUserConfigs.Count);

        var state = await _stateService.ReadAsync(cancellationToken);
        try
        {
            foreach (var userConfig in enabledUserConfigs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Syncing generated playlists for user {Username}", userConfig.UserName);
                await HandleUserPlaylistSync(reporter, userConfig, state, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Generated playlist sync task has been cancelled");
            reporter.Finish();
        }
        finally
        {
            await _stateService.SaveAsync(state, CancellationToken.None);
        }
    }

    private async Task HandleUserPlaylistSync(
        SyncProgress reporter,
        UserConfig userConfig,
        PlaylistSyncState state,
        CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (user is null)
        {
            _logger.LogWarning("User with ID {UserId} does not exist", userConfig.JellyfinUserId);
            reporter.CompleteUser();
            return;
        }

        var discovery = await _discoveryService.DiscoverAsync(userConfig, cancellationToken);
        var discoveredEntries = state.ApplyDiscovery(user.Id, discovery.Playlists);
        if (discoveredEntries.Count == 0)
        {
            reporter.CompleteUser();
            return;
        }

        var candidates = _trackMatcher.GetCandidateAudioItems(user);
        var failedTypes = new HashSet<PlaylistType>();
        foreach (var entry in discoveredEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var succeeded = false;
            try
            {
                succeeded = await SyncEntry(user, userConfig, entry, candidates, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                PlaylistSyncState.RecordFailure(entry, e.Message);
                _logger.LogWarning(
                    "Failed to sync generated playlist {PlaylistId}: {Error}",
                    entry.ListenBrainzPlaylistId,
                    e.Message);
            }

            if (!succeeded)
            {
                AddFailedType(failedTypes, entry);
            }

            reporter.AdvancePlaylist(discoveredEntries.Count);
        }

        // An incomplete discovery is no evidence of a playlist going out of rotation.
        if (!discovery.IsComplete)
        {
            _logger.LogInformation(
                "Discovery for user {Username} was incomplete, skipping playlist cleanup",
                userConfig.UserName);
            return;
        }

        PruneOutOfRotationPlaylists(user, userConfig, state, discovery, failedTypes, cancellationToken);
    }

    /// <summary>
    /// Brings a single discovered playlist up to date.
    /// </summary>
    /// <returns>True if the playlist is up to date after this run, false if it needs a resync.</returns>
    private async Task<bool> SyncEntry(
        User user,
        UserConfig userConfig,
        PlaylistSyncEntry entry,
        IReadOnlyList<BaseItem> candidates,
        CancellationToken cancellationToken)
    {
        var target = ResolveTarget(user, entry);
        if (target.IsUpToDate)
        {
            _logger.LogDebug(
                "Playlist {PlaylistId} is already up to date, skipping",
                entry.ListenBrainzPlaylistId);
            return true;
        }

        _logger.LogDebug(
            "Processing generated playlist {PlaylistId} of type {PlaylistType}",
            entry.ListenBrainzPlaylistId,
            entry.GeneratedType);

        var playlist = await _listenBrainz.GetPlaylistAsync(
            userConfig,
            entry.ListenBrainzPlaylistId,
            cancellationToken);

        return await SyncPlaylist(user, playlist, entry, candidates, target.ExistingPlaylist, cancellationToken);
    }

    private async Task<bool> SyncPlaylist(
        User user,
        Playlist playlist,
        PlaylistSyncEntry entry,
        IReadOnlyList<BaseItem> candidates,
        JellyfinPlaylist? mappedPlaylist,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Syncing generated playlist: {Title}", playlist.Title);

        var tracks = playlist.Tracks.ToList();
        if (tracks.Count == 0)
        {
            _logger.LogDebug("Playlist {Title} has no tracks, skipping", playlist.Title);
            return true;
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
            "Found {Count} (out of {TotalCount}) matching tracks for generated playlist {Title}",
            matchedTracks.Count,
            tracks.Count,
            playlist.Title);

        if (matchedTracks.Count == 0)
        {
            PlaylistSyncState.RecordFailure(entry, "No matching tracks found in the library");
            _logger.LogWarning(
                "No matching tracks for generated playlist {Title}, skipping sync",
                playlist.Title);
            return false;
        }

        var existingPlaylist = mappedPlaylist ?? _playlistManager.FindByName(user, playlist.Title);

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

        PlaylistSyncState.RecordSync(entry, jellyfinPlaylistId);

        _logger.LogInformation(
            "Successfully synced generated playlist {Name} with {Count} tracks",
            playlist.Title,
            matchedTracks.Count);
        return true;
    }

    private SyncTarget ResolveTarget(User user, PlaylistSyncEntry entry)
    {
        if (entry.JellyfinPlaylistId is not Guid jellyfinPlaylistId)
        {
            return new SyncTarget(false, null);
        }

        // A playlist the user cannot see is effectively not synced.
        if (PlaylistTypePolicy.IsUpToDate(entry) &&
            _playlistManager.IsVisibleTo(jellyfinPlaylistId, user.Id))
        {
            return new SyncTarget(true, null);
        }

        var playlist = _playlistManager.FindAny(jellyfinPlaylistId);
        if (playlist is null)
        {
            _logger.LogInformation(
                "Mapped Jellyfin playlist {PlaylistId} for ListenBrainz playlist {ListenBrainzPlaylistId} no longer exists",
                jellyfinPlaylistId,
                entry.ListenBrainzPlaylistId);
            PlaylistSyncState.ClearSyncResult(entry);
        }

        return new SyncTarget(false, playlist);
    }

    private void PruneOutOfRotationPlaylists(
        User user,
        UserConfig userConfig,
        PlaylistSyncState state,
        PlaylistDiscoveryResult discovery,
        HashSet<PlaylistType> failedTypes,
        CancellationToken cancellationToken)
    {
        var selectedPlaylistIds = discovery
            .Playlists
            .Select(p => p.ListenBrainzPlaylistId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var syncedTypes = discovery
            .Playlists
            .Select(p => PlaylistTypePolicy.ParsePlaylistType(p.GeneratedType))
            .Where(t => t is not null && !failedTypes.Contains(t.Value))
            .Select(t => t!.Value)
            .ToHashSet();

        var entriesToRemove = state
            .EntriesFor(user.Id)
            .Where(e => PlaylistTypePolicy.ShouldPruneEntry(e, selectedPlaylistIds, syncedTypes))
            .ToList();

        foreach (var entry in entriesToRemove)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (userConfig.KeepPlaylistsAfterRotation)
            {
                _logger.LogDebug(
                    "Keeping out-of-rotation generated playlist {PlaylistId}, removing its entry",
                    entry.ListenBrainzPlaylistId);
                state.Entries.Remove(entry);
                continue;
            }

            var playlist = entry.JellyfinPlaylistId is Guid playlistId
                ? _playlistManager.FindAny(playlistId)
                : null;

            if (playlist is not null)
            {
                _logger.LogInformation(
                    "Deleting generated playlist {PlaylistName} because it is no longer in rotation",
                    playlist.Name);
                _playlistManager.Delete(playlist);
            }

            state.Entries.Remove(entry);
        }
    }

    private static void AddFailedType(HashSet<PlaylistType> failedTypes, PlaylistSyncEntry entry)
    {
        var type = PlaylistTypePolicy.ParsePlaylistType(entry.GeneratedType);
        if (type is not null)
        {
            failedTypes.Add(type.Value);
        }
    }

    private IDisposable? BeginLogScope()
    {
        return _logger.BeginScope(new Dictionary<string, object> { { "EventId", "SyncGeneratedPlaylistsTask" } });
    }

    /// <summary>
    /// What the sync should do with a discovered ListenBrainz playlist.
    /// </summary>
    /// <param name="IsUpToDate">Whether the playlist can be skipped.</param>
    /// <param name="ExistingPlaylist">
    /// The mapped Jellyfin playlist to write into, or null to look it up by name or create it.
    /// </param>
    private sealed record SyncTarget(bool IsUpToDate, JellyfinPlaylist? ExistingPlaylist);

    /// <summary>
    /// Tracks task progress as an evenly split share per user.
    /// </summary>
    private sealed class SyncProgress
    {
        private readonly IProgress<double> _progress;
        private readonly double _userShare;
        private double _reported;

        public SyncProgress(IProgress<double> progress, int userCount)
        {
            _progress = progress;
            _userShare = 100.0 / userCount;
        }

        public void AdvancePlaylist(int totalPlaylists)
        {
            _reported += _userShare / totalPlaylists;
            _progress.Report(_reported);
        }

        public void CompleteUser()
        {
            _reported += _userShare;
            _progress.Report(_reported);
        }

        public void Finish() => _progress.Report(100);
    }
}
