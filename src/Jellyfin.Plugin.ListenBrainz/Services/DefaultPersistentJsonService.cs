using System.Text.Json;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of <see cref="IPersistentJsonService{T}"/>.
/// </summary>
/// <typeparam name="T">Data type.</typeparam>
public sealed class DefaultPersistentJsonService<T> : IPersistentJsonService<T>, IDisposable
{
    private readonly string _cacheFilePath;
    private readonly SemaphoreSlim _lock;
    private bool _isDisposed;

    /// <summary>
    /// JSON serializer options.
    /// </summary>
    private static readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPersistentJsonService{T}"/> class.
    /// </summary>
    /// <param name="cacheFilePath">Path to the file.</param>
    public DefaultPersistentJsonService(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
        _lock = new SemaphoreSlim(1, 1);
    }

    ~DefaultPersistentJsonService() => Dispose(false);

    /// <inheritdoc />
    public void Save(T data)
    {
        EnsureCacheDirectory();
        _lock.Wait();
        try
        {
            using var stream = File.Create(_cacheFilePath);
            JsonSerializer.Serialize(stream, data, _serializerOptions);
        }
        catch (Exception ex)
        {
            throw new ServiceException("Saving JSON file failed", ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(T data)
    {
        EnsureCacheDirectory();
        await _lock.WaitAsync();
        try
        {
            await using var stream = File.Create(_cacheFilePath);
            await JsonSerializer.SerializeAsync(stream, data, _serializerOptions);
        }
        catch (Exception ex)
        {
            throw new ServiceException("Saving JSON file failed", ex);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public T Read()
    {
        T? data;
        try
        {
            using var stream = File.OpenRead(_cacheFilePath);
            data = JsonSerializer.Deserialize<T>(stream, _serializerOptions);
        }
        catch (Exception ex)
        {
            throw new ServiceException("Failed to read JSON file", ex);
        }
        finally
        {
            _lock.Release();
        }

        if (data is null)
        {
            throw new ServiceException("Failed to deserialize data from JSON file");
        }

        return data;
    }

    /// <inheritdoc />
    public async Task<T> ReadAsync()
    {
        await _lock.WaitAsync();
        T? data;
        try
        {
            await using var stream = File.OpenRead(_cacheFilePath);
            data = await JsonSerializer.DeserializeAsync<T>(stream, _serializerOptions);
        }
        catch (Exception ex)
        {
            throw new ServiceException("Failed to read JSON file", ex);
        }
        finally
        {
            _lock.Release();
        }

        if (data is null)
        {
            throw new ServiceException("Failed to deserialize data from JSON file");
        }

        return data;
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

    private void EnsureCacheDirectory()
    {
        var directory = Path.GetDirectoryName(_cacheFilePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
    }
}
