using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Playlist = Jellyfin.Plugin.ListenBrainz.Api.Models.Playlist;
using Utils = Jellyfin.Plugin.ListenBrainz.Common.Utils;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Jellyfin task for syncing weekly rotation playlists from ListenBrainz.
/// </summary>
public class SyncWeeklyPlaylistsTask : IScheduledTask
{
    private const int RotationPlaylistCount = 2;

    private readonly ILogger _logger;
    private readonly IListenBrainzService _listenBrainz;
    private readonly IMetadataProviderService _metadataProvider;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IPlaylistManager _playlistManager;
    private readonly IPluginConfigService _configService;
    private readonly IPlaylistSyncStateService _stateService;
    private double _progress;
    private double _userCountRatio;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncWeeklyPlaylistsTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="playlistManager">Playlist manager.</param>
    /// <param name="listenBrainz">ListenBrainz service.</param>
    /// <param name="metadataProvider">Metadata provider service.</param>
    /// <param name="configService">Plugin configuration service.</param>
    /// <param name="stateService">Playlist sync state service.</param>
    public SyncWeeklyPlaylistsTask(
        ILoggerFactory loggerFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IPlaylistManager playlistManager,
        IListenBrainzService listenBrainz,
        IMetadataProviderService metadataProvider,
        IPluginConfigService configService,
        IPlaylistSyncStateService stateService)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.SyncWeeklyPlaylistsTask");
        _listenBrainz = listenBrainz;
        _metadataProvider = metadataProvider;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _playlistManager = playlistManager;
        _configService = configService;
        _stateService = stateService;
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

        var playlists = (await _listenBrainz.GetCreatedForPlaylistsAsync(
            userConfig,
            Limits.MaxItemsPerGet,
            cancellationToken)).ToList();

        _logger.LogInformation(
            "Found {Count} playlists created for user {Username}",
            playlists.Count,
            userConfig.UserName);

        var weeklyPlaylists = PickWeeklyRotationPlaylists(playlists, userConfig).ToList();
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

        // todo: process playlists

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
            .Where(m => m.JellyfinUserId == user.Id && ShouldPruneMapping(userConfig, m, rotationIds, rotationTypes))
            .ToList();

        foreach (var mapping in mappingsToRemove)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var playlist = _playlistManager.GetPlaylistForUser(mapping.JellyfinPlaylistId, user.Id);
            if (playlist is not null)
            {
                _logger.LogInformation(
                    "Deleting weekly playlist {PlaylistName} because it is no longer in rotation",
                    playlist.Name);
                _libraryManager.DeleteItem(playlist, new DeleteOptions { DeleteFileLocation = false });
            }

            state.Mappings.Remove(mapping);
        }
    }

    internal static bool ShouldPruneMapping(
        UserConfig userConfig,
        PlaylistMapping mapping,
        HashSet<string> rotationIds,
        HashSet<WeeklyPlaylistType> rotationTypes)
    {
        if (!TryGetWeeklyType(mapping.Category, out var type))
        {
            return false;
        }

        if (!IsPlaylistTypeEnabled(userConfig, type))
        {
            return true;
        }

        return rotationTypes.Contains(type) && !rotationIds.Contains(mapping.ListenBrainzPlaylistId);
    }

    private static bool TryGetWeeklyType(string? category, out WeeklyPlaylistType type)
    {
        return Enum.TryParse(category, ignoreCase: true, out type) && Enum.IsDefined(type);
    }

    /// <summary>
    /// Pick the created-for playlists (current and previous) rotation for each type the user has enabled.
    /// </summary>
    /// <remarks>
    /// ListenBrainz does not provide "current weekly jams" alias, so newest <see cref="Playlist.CreatedAt"/>
    /// is treated as the current week and the next one as the last week one.
    /// </remarks>
    /// <param name="playlists">Playlists created for the user.</param>
    /// <param name="userConfig">User configuration.</param>
    /// <returns>The weekly playlists matching the user settings.</returns>
    internal static IEnumerable<WeeklyPlaylistCandidate> PickWeeklyRotationPlaylists(
        IEnumerable<Playlist> playlists,
        UserConfig userConfig)
    {
        return playlists
            .Select(GetWeeklyPlaylistCandidate)
            .WhereNotNull()
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Playlist.PlaylistId))
            .Where(candidate => IsPlaylistTypeEnabled(userConfig, candidate.Type))
            .GroupBy(candidate => candidate.Type)
            .SelectMany(group => group
                .OrderByDescending(candidate => candidate.Playlist.CreatedAt)
                .ThenByDescending(candidate => candidate.Playlist.Identifier, StringComparer.OrdinalIgnoreCase)
                .Take(RotationPlaylistCount))
            .OrderBy(candidate => candidate.Type)
            .ThenByDescending(candidate => candidate.Playlist.CreatedAt);
    }

    internal static WeeklyPlaylistCandidate? GetWeeklyPlaylistCandidate(Playlist playlist)
    {
        return playlist.JspfPlaylist.SourcePatch switch
        {
            "weekly-jams" => new WeeklyPlaylistCandidate(playlist, WeeklyPlaylistType.Jams),
            "weekly-exploration" => new WeeklyPlaylistCandidate(playlist, WeeklyPlaylistType.Exploration),
            _ => null,
        };
    }

    private static bool IsPlaylistTypeEnabled(UserConfig userConfig, WeeklyPlaylistType playlistType)
    {
        return playlistType switch
        {
            WeeklyPlaylistType.Jams => userConfig.IsWeeklyJamsSyncEnabled,
            WeeklyPlaylistType.Exploration => userConfig.IsWeeklyExplorationSyncEnabled,
            _ => false,
        };
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
