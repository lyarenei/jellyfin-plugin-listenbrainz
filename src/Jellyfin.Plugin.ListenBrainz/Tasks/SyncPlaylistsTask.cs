using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using ClientUtils = Jellyfin.Plugin.ListenBrainz.Clients.Utils;
using Playlist = Jellyfin.Plugin.ListenBrainz.Api.Models.Playlist;
using Utils = Jellyfin.Plugin.ListenBrainz.Common.Utils;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Jellyfin task for syncing playlists from ListenBrainz.
/// </summary>
public class SyncPlaylistsTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IPlaylistManager _playlistManager;
    private readonly IPluginConfigService _configService;
    private double _progress;
    private double _userCountRatio;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncPlaylistsTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="clientFactory">HTTP client factory.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="playlistManager">Playlist manager.</param>
    /// <param name="listenBrainzClient">ListenBrainz client.</param>
    /// <param name="configService">Plugin configuration service.</param>
    public SyncPlaylistsTask(
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IPlaylistManager playlistManager,
        IListenBrainzClient? listenBrainzClient = null,
        IPluginConfigService? configService = null)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.SyncPlaylistsTask");
        _listenBrainzClient = listenBrainzClient ?? ClientUtils.GetListenBrainzClient(_logger, clientFactory);
        _libraryManager = libraryManager;
        _userManager = userManager;
        _playlistManager = playlistManager;
        _configService = configService ?? new DefaultPluginConfigService();
    }

    /// <inheritdoc />
    public string Name => "Sync playlists from ListenBrainz";

    /// <inheritdoc />
    public string Key => "SyncPlaylists";

    /// <inheritdoc />
    public string Description => "Sync playlists from ListenBrainz to Jellyfin";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
    {
        new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.WeeklyTrigger,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDayTicks = Utils.GetRandomMinute() * TimeSpan.TicksPerMinute,
        }
    };

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        using var logScope = BeginLogScope();
        if (_configService.UserConfigs.Count == 0)
        {
            _logger.LogInformation("No users have been configured, nothing to sync");
            progress.Report(100);
            return;
        }

        _logger.LogInformation("Starting playlist sync from ListenBrainz...");
        ResetProgress(_configService.UserConfigs.Count);

        try
        {
            foreach (var userConfig in _configService.UserConfigs)
            {
                _logger.LogInformation("Syncing playlists for user {Username}", userConfig.UserName);
                if (!userConfig.IsPlaylistsSyncEnabled)
                {
                    _logger.LogInformation("User has not playlist syncing enabled, skipping");
                    _progress += _userCountRatio;
                    progress.Report(_progress);
                    continue;
                }

                await HandlePlaylistSync(progress, userConfig, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Playlist sync task has been cancelled");
            progress.Report(100);
        }
    }

    private async Task HandlePlaylistSync(
        IProgress<double> progress,
        UserConfig userConfig,
        CancellationToken cancellationToken)
    {
        var user = _userManager.GetUserById(userConfig.JellyfinUserId);
        if (user is null)
        {
            _logger.LogError("User with ID {UserId} does not exist", userConfig.JellyfinUserId);
            _progress += _userCountRatio;
            progress.Report(_progress);
            return;
        }

        try
        {
            var playlists = (
                await _listenBrainzClient.GetCreatedForPlaylistsAsync(
                    userConfig,
                    25,
                    cancellationToken)
            ).ToList();
            _logger.LogInformation("Found {Count} playlists for user {Username}", playlists.Count, userConfig.UserName);

            if (playlists.Count == 0)
            {
                _progress += _userCountRatio;
                progress.Report(_progress);
                return;
            }

            var playlistRatio = _userCountRatio / playlists.Count;

            foreach (var pl in playlists)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!ShouldSyncPlaylist(pl.JspfPlaylist.SourcePatch) && !_configService.IsAllPlaylistsSyncEnabled)
                {
                    _logger.LogDebug(
                        "Skipping sync of playlist {PlaylistId} of type {PlaylistType}: syncing all playlists is disabled",
                        pl.Identifier,
                        pl.JspfPlaylist.SourcePatch);
                }

                try
                {
                    var playlist = await _listenBrainzClient.GetPlaylistAsync(
                        userConfig,
                        pl.PlaylistId,
                        cancellationToken);
                    await SyncPlaylist(user, playlist, cancellationToken);
                }
                catch (Exception e)
                {
                    _logger.LogWarning("Failed to sync playlist {PlaylistId}: {Error}", pl.Identifier, e.Message);
                }

                _progress += playlistRatio;
                progress.Report(_progress);
            }
        }
        catch (PluginException e)
        {
            _logger.LogError("Failed to fetch playlists for user {Username}: {Error}", userConfig.UserName, e.Message);
            _progress += _userCountRatio;
            progress.Report(_progress);
        }
    }

    private async Task SyncPlaylist(
        User user,
        Playlist playlist,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Syncing playlist: {Title}", playlist.Title);

        var tracks = playlist.Tracks.ToList();
        if (tracks.Count == 0)
        {
            _logger.LogDebug("Playlist {Title} has no tracks, skipping", playlist.Title);
            return;
        }

        // Find Jellyfin items matching the recording MBIDs from the playlist
        var jellyfinPlaylistTracks = new List<BaseItem>();
        var allowedLibraries = GetAllowedLibraries()
            .Select(al => _libraryManager.GetItemById(al))
            .WhereNotNull()
            .ToList();

        var query = new InternalItemsQuery(user) { MediaTypes = [MediaType.Audio] };
        var allAudioItems = _libraryManager.GetItemList(query, allowedLibraries).ToList();

        foreach (var track in tracks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var recordingMbid = track.RecordingMbid;
            if (string.IsNullOrEmpty(recordingMbid))
            {
                _logger.LogDebug("Track {ID} has no recording MBID, skipping", track.Identifier);
                continue;
            }

            // Find item with matching recording MBID
            var item = allAudioItems.FirstOrDefault(i => i.GetRecordingMbid() == recordingMbid);
            if (item is null)
            {
                _logger.LogDebug(
                    "No Jellyfin item found for recording MBID {MBID} (track: {ID})",
                    recordingMbid,
                    track.Identifier);
                continue;
            }

            jellyfinPlaylistTracks.Add(item);
        }

        _logger.LogInformation(
            "Found {Count} (out of {TotalCount}) matching tracks for playlist {Title}",
            jellyfinPlaylistTracks.Count,
            playlist.Tracks.Count(),
            playlist.Title);

        var playlistName = $"[LB] {playlist.Title}";
        var playlistQuery = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Playlist], Name = playlistName, User = user,
        };
        var existingPlaylist = _libraryManager.GetItemList(playlistQuery).FirstOrDefault();
        if (existingPlaylist is not null)
        {
            _logger.LogDebug("Deleting already existing playlist {Title}", playlist.Title);
            _libraryManager.DeleteItem(
                existingPlaylist,
                new DeleteOptions { DeleteFileLocation = false });
        }

        _logger.LogDebug("Creating playlist {Name} with {Count} items", playlistName, jellyfinPlaylistTracks.Count);
        await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
        {
            Name = playlistName,
            UserId = user.Id,
            ItemIdList = jellyfinPlaylistTracks.Select(i => i.Id).ToArray(),
            MediaType = MediaType.Audio
        });

        _logger.LogInformation(
            "Successfully synced playlist {Name} with {Count} tracks",
            playlistName,
            jellyfinPlaylistTracks.Count);
    }

    private IEnumerable<Guid> GetAllowedLibraries()
    {
        var allLibraries = _configService.LibraryConfigs;
        if (allLibraries.Count > 0)
        {
            return allLibraries.Where(lc => lc.IsAllowed).Select(lc => lc.Id);
        }

        return _libraryManager.GetMusicLibraries().Select(ml => ml.Id);
    }

    private void ResetProgress(int userCount)
    {
        _userCountRatio = 100.0 / userCount;
        _progress = 0;
    }

    private IDisposable? BeginLogScope()
    {
        return _logger.BeginScope(new Dictionary<string, object> { { "EventId", "SyncPlaylistsTask" } });
    }

    private static bool ShouldSyncPlaylist(string sourcePatch)
    {
        string[] allowedPatches =
        [
            "weekly-jams",
            "top-discoveries-of",
            "top-discoveries-for-year",
            "top-new-recordings-for-year",
            "top-recordings-for-year"
        ];

        return allowedPatches.Any(patch => sourcePatch.Contains(patch, StringComparison.InvariantCultureIgnoreCase));
    }
}
