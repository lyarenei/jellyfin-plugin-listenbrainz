using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Utils.Services;

public sealed class DummyPersistentJsonService : IPersistentJsonService<Dictionary<Guid, List<StoredListen>>>
{
    public Dictionary<Guid, List<StoredListen>>? ReadData { get; set; }

    public Dictionary<Guid, List<StoredListen>>? LastSavedData { get; private set; }

    public int SaveAsyncCalls { get; private set; }

    public void Save(Dictionary<Guid, List<StoredListen>> data, string? filePath = null) => LastSavedData = Clone(data);

    public Task SaveAsync(
        Dictionary<Guid, List<StoredListen>> data,
        string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        SaveAsyncCalls++;
        LastSavedData = Clone(data);
        return Task.CompletedTask;
    }

    public Dictionary<Guid, List<StoredListen>> Read(string? filePath = null)
        => ReadData ?? new Dictionary<Guid, List<StoredListen>>();

    public Task<Dictionary<Guid, List<StoredListen>>> ReadAsync(
        string? filePath = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Read(filePath));

    private static Dictionary<Guid, List<StoredListen>> Clone(Dictionary<Guid, List<StoredListen>> data)
    {
        return data.ToDictionary(
            pair => pair.Key,
            pair => pair.Value
                .Select(sl => new StoredListen
                {
                    Id = sl.Id,
                    ListenedAt = sl.ListenedAt,
                    Metadata = sl.Metadata,
                })
                .ToList());
    }
}
