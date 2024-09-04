using System.Text.Json;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Managers;

/// <summary>
/// Listens backup manager.
/// </summary>
public class BackupManager : IDisposable
{
    private static BackupManager? _instance;
    private readonly SemaphoreSlim _lock;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BackupManager"/> class.
    /// </summary>
    public BackupManager()
    {
        _lock = new SemaphoreSlim(1, 1);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="BackupManager"/> class.
    /// </summary>
    ~BackupManager() => Dispose(false);

    /// <summary>
    /// Gets instance of the backup manager.
    /// </summary>
    public static BackupManager Instance => _instance ??= new BackupManager();

    /// <summary>
    /// Back up listen of a current item.
    /// </summary>
    /// <param name="userName">ListenBrainz username.</param>
    /// <param name="item">Listened item.</param>
    /// <param name="metadata">Item metadata.</param>
    /// <param name="timestamp">Listen timestamp.</param>
    /// <exception cref="PluginException">Backup failed.</exception>
    public void Backup(string userName, Audio item, AudioItemMetadata? metadata, long timestamp)
    {
        var config = Plugin.GetConfiguration();
        var filePath = Path.Combine(config.BackupPath, userName, DateTime.Today.ToShortDateString());
        List<Listen>? userListens = null;

        _lock.Wait();
        try
        {
            using var stream = File.OpenRead(filePath);
            userListens = JsonSerializer.Deserialize<List<Listen>>(stream);
        }
        catch (FileNotFoundException)
        {
        }
        catch (Exception ex)
        {
            throw new PluginException("Failed to read backup file", ex);
        }

        userListens ??= new List<Listen>();
        userListens.Add(item.AsListen(timestamp, metadata));

        try
        {
            using var stream = File.Create(filePath);
            JsonSerializer.Serialize(stream, userListens);
        }
        catch (Exception ex)
        {
            throw new PluginException("Listen backup failed", ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose managed and unmanaged (own) resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    private void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (disposing)
        {
            _lock.Dispose();
        }

        _isDisposed = true;
    }
}
