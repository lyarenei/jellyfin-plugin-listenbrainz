using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Jellyfin media library.
/// </summary>
public class JellyfinMediaLibrary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMediaLibrary"/> class.
    /// </summary>
    public JellyfinMediaLibrary()
    {
        Name = string.Empty;
        LibraryType = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMediaLibrary"/> class.
    /// </summary>
    /// <param name="item">Jellyfin item/folder.</param>
    public JellyfinMediaLibrary(CollectionFolder item)
    {
        Name = item.Name;
        Id = item.Id;

        // Collection type is null when library content type is mixed"
        LibraryType = item.CollectionType?.ToString() ?? "Mixed";
    }

    /// <summary>
    /// Gets or sets library name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets library ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets library type.
    /// </summary>
    public string LibraryType { get; set; }
}
