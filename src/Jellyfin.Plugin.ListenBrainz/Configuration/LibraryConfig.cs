namespace Jellyfin.Plugin.ListenBrainz.Configuration;

/// <summary>
/// Jellyfin media library configuration for plugin.
/// </summary>
public class LibraryConfig
{
    /// <summary>
    /// Gets or sets Jellyfin library ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this library should be ignored by plugin.
    /// </summary>
    public bool IsExcluded { get; set; }
}
