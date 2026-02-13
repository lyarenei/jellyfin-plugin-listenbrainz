using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
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
    private static readonly string[] _defaultAllowedPatches =
    [
        "weekly-jams",
        "top-discoveries-of",
        "top-discoveries-for-year",
        "top-new-recordings-for-year",
        "top-recordings-for-year",
    ];

    private readonly ILogger _logger;
    private readonly IListenBrainzClient _listenBrainzClient;
    private readonly IMusicBrainzClient _musicBrainzClient;
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
    /// <param name="musicBrainzClient">MusicBrainz client.</param>
    /// <param name="configService">Plugin configuration service.</param>
    public SyncPlaylistsTask(
        ILoggerFactory loggerFactory,
        IHttpClientFactory clientFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IPlaylistManager playlistManager,
        IListenBrainzClient? listenBrainzClient = null,
        IMusicBrainzClient? musicBrainzClient = null,
        IPluginConfigService? configService = null)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.SyncPlaylistsTask");
        _listenBrainzClient = listenBrainzClient ?? ClientUtils.GetListenBrainzClient(_logger, clientFactory);
        _musicBrainzClient = musicBrainzClient ?? ClientUtils.GetMusicBrainzClient(_logger, clientFactory);
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
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() =>
    [
        new()
        {
            Type = TaskTriggerInfo.TriggerWeekly,
            DayOfWeek = DayOfWeek.Monday,
            TimeOfDayTicks = Utils.GetRandomMinute() * TimeSpan.TicksPerMinute,
        },
    ];

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
                    Limits.MaxItemsPerGet,
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

            var allowedLibraries = GetAllowedLibraries()
                .Select(al => _libraryManager.GetItemById(al))
                .WhereNotNull()
                .ToList();

            var query = new InternalItemsQuery(user) { MediaTypes = [MediaType.Audio] };
            var allAudioItems = _libraryManager.GetItemList(query, allowedLibraries).ToList();

            foreach (var pl in playlists)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug(
                    "Processing playlist {PlaylistId} of type {PlaylistType}",
                    pl.Identifier,
                    pl.JspfPlaylist.SourcePatch);

                if (_configService.IsAllPlaylistsSyncEnabled || ShouldSyncPlaylist(pl.JspfPlaylist.SourcePatch))
                {
                    try
                    {
                        var playlist = await _listenBrainzClient.GetPlaylistAsync(
                            userConfig,
                            pl.PlaylistId,
                            cancellationToken);
                        await SyncPlaylist(user, playlist, allAudioItems, cancellationToken);
                    }
                    catch (Exception e)
                    {
                        _logger.LogWarning("Failed to sync playlist {PlaylistId}: {Error}", pl.Identifier, e.Message);
                    }
                }
                else
                {
                    _logger.LogDebug(
                        "Skipping sync of playlist {PlaylistId} of type {PlaylistType}: syncing all playlists is disabled",
                        pl.Identifier,
                        pl.JspfPlaylist.SourcePatch);
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
        IReadOnlyList<BaseItem> allAudioItems,
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

        foreach (var track in tracks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var item = await FindJellyfinItem(allAudioItems, user, track, cancellationToken);
            if (item is null)
            {
                _logger.LogDebug(
                    "No Jellyfin item found for track: {Title} (ID: {ID})",
                    track.Title,
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
            IncludeItemTypes = [BaseItemKind.Playlist],
            Name = playlistName,
            User = user,
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
        return _defaultAllowedPatches.Any(patch =>
            sourcePatch.Contains(patch, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task<BaseItem?> FindJellyfinItem(
        IReadOnlyList<BaseItem> allAudioItems,
        User user,
        PlaylistTrack track,
        CancellationToken cancellationToken)
    {
        // 1. Best scenario: Exact match by recording MBID
        if (!string.IsNullOrEmpty(track.RecordingMbid))
        {
            var item = allAudioItems.FirstOrDefault(i => i.GetRecordingMbid() == track.RecordingMbid);
            if (item is not null)
            {
                _logger.LogDebug("Matched track '{Title}' by recording MBID", track.Title);
                return item;
            }
        }

        // 2. Match by Album MBID + Title
        if (!string.IsNullOrEmpty(track.ReleaseMbid))
        {
            var item = allAudioItems.FirstOrDefault(i =>
                i.ProviderIds.TryGetValue("MusicBrainzAlbum", out var albumMbid) &&
                albumMbid == track.ReleaseMbid &&
                i.Name.Equals(track.Title, StringComparison.OrdinalIgnoreCase));
            if (item is not null)
            {
                _logger.LogDebug("Matched track '{Title}' by album MBID + title", track.Title);
                return item;
            }
        }

        // 3. MusicBrainz related recordings (only if MBID available and MusicBrainz enabled)
        if (!string.IsNullOrEmpty(track.RecordingMbid) && _configService.IsMusicBrainzEnabled)
        {
            _logger.LogDebug("Looking up related recordings for recording MBID {Mbid}", track.RecordingMbid);

            var relatedRecordingMbids = await _musicBrainzClient.GetRelatedRecordingMbidsAsync(
                track.RecordingMbid,
                cancellationToken);

            foreach (var relatedMbid in relatedRecordingMbids)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var item = allAudioItems.FirstOrDefault(i => i.GetRecordingMbid() == relatedMbid);
                if (item is not null)
                {
                    _logger.LogDebug(
                        "Matched track '{Title}' by related recording MBID {Mbid}",
                        track.Title,
                        relatedMbid);
                    return item;
                }
            }
        }

        // At this point, no matching with IDs can be done, so try plaintext matching
        // TODO: make this configurable
        var searchCandidates = SearchJellyfinItems(user, track.Title);

        // 4. Artist + Title search
        if (!string.IsNullOrEmpty(track.Creator))
        {
            var item = searchCandidates.FirstOrDefault(i => ArtistMatches(i, track.Creator));
            if (item is not null)
            {
                _logger.LogDebug("Matched track '{Title}' by artist + title search", track.Title);
                return item;
            }
        }

        // 5. Album name + Title search
        if (!string.IsNullOrEmpty(track.Album))
        {
            var item = searchCandidates
                .OfType<Audio>()
                .FirstOrDefault(i => i.Album?.Equals(track.Album, StringComparison.OrdinalIgnoreCase) == true);
            if (item is not null)
            {
                _logger.LogDebug("Matched track '{Title}' by album name + title search", track.Title);
                return item;
            }
        }

        // 6. Title-only search would lead to too many false positives so no point in doing that...

        _logger.LogDebug("No match found for track '{Title}'", track.Title);
        return null;
    }

    private IReadOnlyList<BaseItem> SearchJellyfinItems(User user, string searchTerm)
    {
        var searchItems = _libraryManager.GetItemList(new InternalItemsQuery(user)
        {
            MediaTypes = [MediaType.Audio],
            SearchTerm = searchTerm,
        });

        if (searchItems.Count == 0)
        {
            _logger.LogDebug("No tracks found for search term '{Term}'", searchTerm);
        }
        else
        {
            _logger.LogDebug("Found {Count} tracks for search term '{Term}'", searchItems.Count, searchTerm);
        }

        return searchItems;
    }

    private static bool ArtistMatches(BaseItem item, string artistName)
    {
        // Handle "Artist feat. Other" format - check if any artist is contained in creator string
        if (item is not Audio audio || audio.Artists is null)
        {
            return false;
        }

        return audio.Artists
            .TakeWhile(a => !string.IsNullOrEmpty(a))
            .Any(a => artistName.Contains(a, StringComparison.OrdinalIgnoreCase));
    }
}
