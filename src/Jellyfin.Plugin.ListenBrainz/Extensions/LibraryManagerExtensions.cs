using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.ListenBrainz.Extensions;

/// <summary>
/// Extensions for library manager.
/// </summary>
public static class LibraryManagerExtensions
{
    /// <summary>
    /// Get all music libraries.
    /// </summary>
    /// <param name="libraryManager">Jellyfin library manager.</param>
    /// <returns>All music libraries on server.</returns>
    public static IEnumerable<Folder> GetMusicLibraries(this ILibraryManager libraryManager)
    {
        return libraryManager
            .GetUserRootFolder()
            .Children
            .Cast<CollectionFolder>()
            .Where(f => f.CollectionType == "music");
    }
}
