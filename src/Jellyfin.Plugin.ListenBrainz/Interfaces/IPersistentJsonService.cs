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
    /// <param name="filePath">Write data to this file.</param>
    public void Save(T data, string? filePath = null);

    /// <summary>
    /// Save data to a file on disk.
    /// </summary>
    /// <param name="data">Data to save.</param>
    /// <param name="filePath">Write data to this file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing asynchronous operation.</returns>
    public Task SaveAsync(T data, string? filePath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Read data from a file on disk.
    /// </summary>
    /// <param name="filePath">Read data from this file.</param>
    /// <returns>Data read from file.</returns>
    public T Read(string? filePath = null);

    /// <summary>
    /// Read data from a file on disk.
    /// </summary>
    /// <param name="filePath">Read data from this file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Data read from file.</returns>
    public Task<T> ReadAsync(string? filePath = null, CancellationToken cancellationToken = default);
}
