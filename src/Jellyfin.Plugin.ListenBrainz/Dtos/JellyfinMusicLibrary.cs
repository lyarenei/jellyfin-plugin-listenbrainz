using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Jellyfin music library.
/// </summary>
public class JellyfinMusicLibrary
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMusicLibrary"/> class.
    /// </summary>
    public JellyfinMusicLibrary()
    {
        Name = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyfinMusicLibrary"/> class.
    /// </summary>
    /// <param name="item">Jellyfin item/folder.</param>
    public JellyfinMusicLibrary(BaseItem item)
    {
        Name = item.Name;
        Id = item.Id;
    }

    /// <summary>
    /// Gets or sets library name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets library id.
    /// </summary>
    public Guid Id { get; set; }
}
