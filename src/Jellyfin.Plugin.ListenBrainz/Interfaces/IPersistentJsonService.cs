namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Json persistence service.
/// </summary>
/// <typeparam name="T">Data type.</typeparam>
public interface IPersistentJsonService<T>
{
    /// <summary>
    /// Save data to a file on disk.
    /// </summary>
    /// <param name="data">Data to save.</param>
    public void Save(T data);

    /// <summary>
    /// Save data to a file on disk.
    /// </summary>
    /// <param name="data">Data to save.</param>
    /// <returns>Task representing asynchronous operation.</returns>
    public Task SaveAsync(T data);

    /// <summary>
    /// Read data from a file on disk.
    /// </summary>
    /// <returns>Data read from file.</returns>
    public T Read();

    /// <summary>
    /// Read data from a file on disk.
    /// </summary>
    /// <returns>Data read from file.</returns>
    public Task<T> ReadAsync();
}
