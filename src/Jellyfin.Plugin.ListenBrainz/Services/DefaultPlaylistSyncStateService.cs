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
        try
        {
            return await _storage.ReadAsync(cancellationToken: cancellationToken);
        }
        catch (ServiceException e)
        {
            _logger.LogWarning("Failed to read playlist sync state, starting fresh: {Error}", e.Message);
            return new PlaylistSyncState();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(PlaylistSyncState state, CancellationToken cancellationToken)
    {
        await _storage.SaveAsync(state, cancellationToken: cancellationToken);
    }
}
