namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Cache manager.
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Save cache content to a file on disk.
    /// </summary>
    public void Save();

    /// <summary>
    /// Restore cache content from a file on disk.
    /// </summary>
    public void Restore();
}
