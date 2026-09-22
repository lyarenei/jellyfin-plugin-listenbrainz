using System.Text.Json;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of <see cref="IPlaylistSyncStateService"/>.
/// </summary>
public class DefaultPlaylistSyncStateService : IPlaylistSyncStateService
{
    private readonly ILogger _logger;
    private readonly IPersistentJsonService<PlaylistSyncState> _storage;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPlaylistSyncStateService"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="storage">Persistent JSON storage.</param>
    public DefaultPlaylistSyncStateService(
        ILogger logger,
        IPersistentJsonService<PlaylistSyncState> storage)
    {
        _logger = logger;
        _storage = storage;
    }

    /// <inheritdoc />
    public async Task<PlaylistSyncState> ReadAsync(CancellationToken cancellationToken)
    {
        PlaylistSyncState state;
        try
        {
            state = await _storage.ReadAsync(cancellationToken: cancellationToken);
        }
        catch (ServiceException e) when (e.InnerException is FileNotFoundException or DirectoryNotFoundException)
        {
            _logger.LogInformation("No playlist sync state found, starting fresh: {Error}", e.Message);
            return new PlaylistSyncState();
        }
        catch (ServiceException e) when (e.InnerException is JsonException)
        {
            // The state is derived, so discarding it costs a resync and nothing else.
            _logger.LogWarning("Playlist sync state is corrupt and will be rebuilt: {Error}", e.Message);
            return new PlaylistSyncState();
        }

        if (state.Version != PlaylistSyncState.CurrentVersion)
        {
            _logger.LogInformation(
                "Playlist sync state has version {Version}, expected {Expected}; rebuilding it",
                state.Version,
                PlaylistSyncState.CurrentVersion);
            return new PlaylistSyncState();
        }

        return state;
    }

    /// <inheritdoc />
    public async Task SaveAsync(PlaylistSyncState state, CancellationToken cancellationToken)
    {
        state.Version = PlaylistSyncState.CurrentVersion;
        await _storage.SaveAsync(state, cancellationToken: cancellationToken);
    }
}
