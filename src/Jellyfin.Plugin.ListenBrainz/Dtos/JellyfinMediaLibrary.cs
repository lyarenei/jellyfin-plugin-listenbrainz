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
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMediaLibrary"/> class.
    /// </summary>
    /// <param name="item">Jellyfin item/folder.</param>
    public JellyfinMediaLibrary(BaseItem item)
    {
        Name = item.Name;
        Id = item.Id;
    }

    /// <summary>
    /// Gets or sets library name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets library ID.
    /// </summary>
    public Guid Id { get; set; }
}
