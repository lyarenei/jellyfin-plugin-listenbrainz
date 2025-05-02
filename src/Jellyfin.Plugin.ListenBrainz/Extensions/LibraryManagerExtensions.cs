using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ListenBrainz.Extensions;

/// <summary>
/// Extensions for library manager.
/// </summary>
public static class LibraryManagerExtensions
{
    /// <summary>
    /// Get all libraries.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <returns>All music libraries on server.</returns>
    public static IEnumerable<Folder> GetLibraries(this ILibraryManager libraryManager)
    {
        return libraryManager
            .GetUserRootFolder()
            .Children
            .Cast<Folder>();
    }

    /// <summary>
    /// Get all music libraries.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <returns>All music libraries on server.</returns>
    public static IEnumerable<Folder> GetMusicLibraries(this ILibraryManager libraryManager)
    {
        return GetLibraries(libraryManager)
            .Cast<CollectionFolder>()
            .Where(f => f.CollectionType == CollectionType.music);
    }

    /// <summary>
    /// Convert StoredListen to Listen.
    /// </summary>
    /// <param name="libraryManager">Library manager instance.</param>
    /// <param name="listen">Stored listen to convert.</param>
    /// <returns>Listen corresponding to provided stored listen. Null if conversion failed.</returns>
    public static Listen? ToListen(this ILibraryManager libraryManager, StoredListen listen)
    {
        var baseItem = libraryManager.GetItemById(listen.Id);
        try
        {
            var audio = (Audio?)baseItem;
            return audio?.AsListen(listen.ListenedAt, listen.Metadata);
        }
        catch (InvalidCastException)
        {
            return null;
        }
    }
}
