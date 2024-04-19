using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
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
}
