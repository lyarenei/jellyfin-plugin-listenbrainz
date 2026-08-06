using System.Globalization;
using System.Reflection;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Playlists;
using Microsoft.Extensions.Logging;
using IPlaylistManager = Jellyfin.Plugin.ListenBrainz.Interfaces.IPlaylistManager;
using JellyfinPlaylist = MediaBrowser.Controller.Playlists.Playlist;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of <see cref="Interfaces.IPlaylistManager"/>.
/// </summary>
public class DefaultPlaylistManager : IPlaylistManager
{
    private const string PlaylistTag = "ListenBrainz";

    // For Jellyfin 12.x
    private static readonly MethodInfo? _addItemToPlaylistWithPosition = typeof(MediaBrowser.Controller.Playlists.IPlaylistManager)
        .GetMethod(
            "AddItemToPlaylistAsync",
            [typeof(Guid), typeof(IReadOnlyCollection<Guid>), typeof(int?), typeof(Guid)]);

    // For Jellyfin 10.11.x
    private static readonly MethodInfo? _addItemToPlaylistLegacy = typeof(MediaBrowser.Controller.Playlists.IPlaylistManager)
        .GetMethod(
            "AddItemToPlaylistAsync",
            [typeof(Guid), typeof(IReadOnlyCollection<Guid>), typeof(Guid)]);

    private readonly ILogger _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly MediaBrowser.Controller.Playlists.IPlaylistManager _playlistManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPlaylistManager"/> class.
    /// </summary>
    /// <param name="logger">Logger.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="playlistManager">Jellyfin playlist manager.</param>
    public DefaultPlaylistManager(
        ILogger logger,
        ILibraryManager libraryManager,
        MediaBrowser.Controller.Playlists.IPlaylistManager playlistManager)
    {
        _logger = logger;
        _libraryManager = libraryManager;
        _playlistManager = playlistManager;
    }

    /// <inheritdoc />
    public JellyfinPlaylist? Find(Guid playlistId, Guid userId)
        => _playlistManager.GetPlaylistForUser(playlistId, userId);

    /// <inheritdoc />
    public JellyfinPlaylist? FindByName(User user, string name)
    {
        var query = new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Playlist],
            Name = name,
            User = user,
        };

        return _libraryManager
            .GetItemList(query)
            .OfType<JellyfinPlaylist>()
            .FirstOrDefault(HasListenBrainzTag);
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(
        User user,
        string title,
        IReadOnlyList<BaseItem> tracks,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating playlist {Name} with {Count} items", title, tracks.Count);
        var createdPlaylist = await _playlistManager.CreatePlaylist(new PlaylistCreationRequest
        {
            Name = title,
            UserId = user.Id,
            ItemIdList = tracks.Select(i => i.Id).ToArray(),
            MediaType = MediaType.Audio,
        });

        if (!Guid.TryParse(createdPlaylist.Id, out var playlistId))
        {
            throw new PluginException($"Created playlist ID '{createdPlaylist.Id}' is invalid");
        }

        await TagPlaylist(playlistId, user.Id, cancellationToken);
        return playlistId;
    }

    /// <inheritdoc />
    public async Task ReplaceTracksAsync(
        User user,
        JellyfinPlaylist playlist,
        IReadOnlyList<BaseItem> tracks,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating playlist {Name} with {Count} items", playlist.Name, tracks.Count);

        var entryIds = playlist
            .GetLinkedChildrenInfos()
            .Select(i => i.Item1.ItemId)
            .Where(id => id is not null)
            .Select(id => id!.Value.ToString("N", CultureInfo.InvariantCulture))
            .ToArray();

        if (entryIds.Length > 0)
        {
            await _playlistManager.RemoveItemFromPlaylistAsync(
                playlist.Id.ToString("N", CultureInfo.InvariantCulture),
                entryIds);
        }

        await AddItemsToPlaylistAsync(playlist.Id, tracks.Select(i => i.Id).ToArray(), user.Id);

        await TagPlaylist(playlist.Id, user.Id, cancellationToken);
    }

    /// <inheritdoc />
    public void Delete(JellyfinPlaylist playlist)
    {
        _libraryManager.DeleteItem(playlist, new DeleteOptions { DeleteFileLocation = false });
    }

    private Task AddItemsToPlaylistAsync(Guid playlistId, IReadOnlyCollection<Guid> itemIds, Guid userId)
    {
        // Not null => running on Jellyfin 12.x
        if (_addItemToPlaylistWithPosition is not null)
        {
            return (Task)_addItemToPlaylistWithPosition.Invoke(
                _playlistManager,
                [playlistId, itemIds, null, userId])!;
        }

        // Not null => running on Jellyfin 10.11.x
        if (_addItemToPlaylistLegacy is not null)
        {
            return (Task)_addItemToPlaylistLegacy.Invoke(
                _playlistManager,
                [playlistId, itemIds, userId])!;
        }

        _logger.LogDebug("Incompatible Jellyfin version: no matching IPlaylistManager.AddItemToPlaylistAsync overload is available");
        throw new PluginException("Incompatible Jellyfin version");
    }

    private async Task TagPlaylist(Guid playlistId, Guid userId, CancellationToken cancellationToken)
    {
        var playlist = _playlistManager.GetPlaylistForUser(playlistId, userId);
        if (playlist is null)
        {
            _logger.LogWarning("Could not tag playlist {PlaylistId}: playlist was not found", playlistId);
            return;
        }

        if (HasListenBrainzTag(playlist))
        {
            return;
        }

        playlist.Tags = [..playlist.Tags, PlaylistTag];
        await playlist.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
    }

    private static bool HasListenBrainzTag(BaseItem playlist)
    {
        return playlist.Tags.Any(tag => tag.Equals(PlaylistTag, StringComparison.OrdinalIgnoreCase));
    }
}
