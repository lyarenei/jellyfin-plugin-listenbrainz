using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Tasks.SyncGeneratedPlaylists;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of <see cref="IPlaylistDiscoveryService"/>.
/// </summary>
public class DefaultPlaylistDiscoveryService : IPlaylistDiscoveryService
{
    private readonly ILogger _logger;
    private readonly IListenBrainzService _listenBrainz;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPlaylistDiscoveryService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="listenBrainz">ListenBrainz service.</param>
    public DefaultPlaylistDiscoveryService(ILogger logger, IListenBrainzService listenBrainz)
    {
        _logger = logger;
        _listenBrainz = listenBrainz;
    }

    /// <inheritdoc />
    public async Task<PlaylistDiscoveryResult> DiscoverAsync(UserConfig userConfig, CancellationToken cancellationToken)
    {
        List<Api.Models.Playlist> playlists;
        try
        {
            playlists = (await _listenBrainz.GetCreatedForPlaylistsAsync(
                userConfig,
                Limits.MaxItemsPerGet,
                cancellationToken)).ToList();
        }
        catch (Exception e) when (e is ServiceException or PluginException)
        {
            _logger.LogError(
                "Failed to fetch generated playlists for user {Username}: {Error}",
                userConfig.UserName,
                e.Message);
            return PlaylistDiscoveryResult.Failed;
        }

        _logger.LogInformation(
            "Found {Count} playlists created for user {Username}",
            playlists.Count,
            userConfig.UserName);

        var selected = PlaylistTypePolicy
            .SelectPlaylists(playlists, userConfig)
            .Select(candidate => new DiscoveredPlaylist(
                candidate.Playlist.PlaylistId,
                PlaylistOrigin.Generated,
                PlaylistTypePolicy.CategoryFor(candidate.Type),
                candidate.Playlist.Title,
                candidate.Playlist.CreatedAt))
            .ToList();

        _logger.LogInformation(
            "Selected {Count} generated playlists for user {Username}",
            selected.Count,
            userConfig.UserName);

        return new PlaylistDiscoveryResult(selected, true);
    }
}
