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
    /// Save cache content to a file on disk.
    /// </summary>
    /// <returns>Task representing asynchronous operation.</returns>
    public Task SaveAsync();

    /// <summary>
    /// Restore cache content from a file on disk.
    /// </summary>
    public void Restore();

    /// <summary>
    /// Restore cache content from a file on disk.
    /// </summary>
    /// <returns>Task representing asynchronous operation.</returns>
    public Task RestoreAsync();
}
