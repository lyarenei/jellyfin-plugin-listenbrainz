using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Common;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of the <see cref="IListenBackupService"/>.
/// </summary>
public class DefaultListenBackupService : IListenBackupService, IDisposable
{
    private readonly ILogger _logger;
    private readonly IPersistentJsonService<List<Listen>> _storage;
    private readonly SemaphoreSlim _lock;
    private readonly string _backupBasePath;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultListenBackupService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="backupBasePath">Path to base backup directory.</param>
    /// <param name="storage">Persistent storage.</param>
    public DefaultListenBackupService(
        ILogger logger,
        string backupBasePath,
        IPersistentJsonService<List<Listen>> storage)
    {
        _logger = logger;
        _lock = new SemaphoreSlim(1, 1);
        _backupBasePath = backupBasePath;
        _storage = storage;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="DefaultListenBackupService"/> class.
    /// </summary>
    ~DefaultListenBackupService() => Dispose(false);

    /// <inheritdoc />
    public async Task Backup(
        string userName,
        Audio item,
        AudioItemMetadata? metadata,
        long timestamp,
        CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await DoBackup(userName, item, metadata, timestamp, cancellationToken);
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
    /// Dispose unmanaged (own) and optionally managed resources.
    /// </summary>
    /// <param name="disposing">Dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
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

    private async Task DoBackup(
        string userName,
        Audio item,
        AudioItemMetadata? metadata,
        long timestamp,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var filePath = Path.Combine(_backupBasePath, userName, $"{DateUtils.TodayIso}.json");
        List<Listen>? userListens = null;

        _logger.LogDebug("Backing up listen of {SongName} to {FileName}", item.Name, filePath);

        try
        {
            userListens = await _storage.ReadAsync(filePath, cancellationToken);
        }
        catch (ServiceException ex) when (ex.InnerException is FileNotFoundException or DirectoryNotFoundException)
        {
            _logger.LogDebug("Backup file does not exist, it will be created");
        }
        catch (Exception ex)
        {
            throw new ServiceException("Failed to read backup file", ex);
        }

        userListens ??= new List<Listen>();
        userListens.Add(item.AsListen(timestamp, metadata));

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _storage.SaveAsync(userListens, filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new ServiceException("Listen backup failed", ex);
        }
    }
}
