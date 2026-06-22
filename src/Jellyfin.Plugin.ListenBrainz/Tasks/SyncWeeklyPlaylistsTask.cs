using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Utils = Jellyfin.Plugin.ListenBrainz.Common.Utils;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Jellyfin task for syncing weekly playlists from ListenBrainz.
/// </summary>
public class SyncWeeklyPlaylistsTask : IScheduledTask
{
    private readonly ILogger _logger;
    private readonly IListenBrainzService _listenBrainz;
    private readonly IMetadataProviderService _metadataProvider;
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;
    private readonly IPlaylistManager _playlistManager;
    private readonly IPluginConfigService _configService;
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
    public SyncWeeklyPlaylistsTask(
        ILoggerFactory loggerFactory,
        ILibraryManager libraryManager,
        IUserManager userManager,
        IPlaylistManager playlistManager,
        IListenBrainzService listenBrainz,
        IMetadataProviderService metadataProvider,
        IPluginConfigService configService)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.SyncWeeklyPlaylistsTask");
        _listenBrainz = listenBrainz;
        _metadataProvider = metadataProvider;
        _libraryManager = libraryManager;
        _userManager = userManager;
        _playlistManager = playlistManager;
        _configService = configService;
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

        try
        {
            foreach (var userConfig in enabledUserConfigs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Syncing weekly playlists for user {Username}", userConfig.UserName);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Weekly playlist sync task has been cancelled");
            progress.Report(100);
        }
    }

    private void ResetProgress(int userCount)
    {
        _userCountRatio = 100.0 / userCount;
        _progress = 0;
    }

    private IDisposable? BeginLogScope()
    {
        return _logger.BeginScope(new Dictionary<string, object> { { "EventId", "SyncWeeklyPlaylistsTask" } });
    }
}
