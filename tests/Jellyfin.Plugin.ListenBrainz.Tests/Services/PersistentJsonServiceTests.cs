using System.Text.Json;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Services;
using Jellyfin.Plugin.ListenBrainz.Tests.TestKit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public sealed class PersistentJsonServiceTests : IDisposable
{
    private readonly TempDir _tempDir = new("lb-json");

    public void Dispose() => _tempDir.Dispose();

    [Fact]
    public async Task SaveAsync_ThenReadAsync_RoundTrips()
    {
        using var service = NewService();

        await service.SaveAsync(StateWith("mbid"));
        var restored = await service.ReadAsync();

        Assert.Equal("mbid", Assert.Single(restored.Entries).ListenBrainzPlaylistId);
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTemporaryFileBehind()
    {
        using var service = NewService();

        await service.SaveAsync(new PlaylistSyncState());

        Assert.True(File.Exists(StatePath));
        Assert.Empty(StagingFiles());
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        using var service = NewService();

        await service.SaveAsync(StateWith("old"));
        await service.SaveAsync(StateWith("new"));
        var restored = await service.ReadAsync();

        Assert.Equal("new", Assert.Single(restored.Entries).ListenBrainzPlaylistId);
    }

    [Fact]
    public void Save_ThenRead_RoundTrips()
    {
        using var service = NewService();

        service.Save(StateWith("mbid"));
        var restored = service.Read();

        Assert.Equal("mbid", Assert.Single(restored.Entries).ListenBrainzPlaylistId);
        Assert.Empty(StagingFiles());
    }

    [Fact]
    public async Task ReadAsync_Throws_WhenFileIsMissing()
    {
        using var service = NewService();

        await Assert.ThrowsAsync<ServiceException>(() => service.ReadAsync());
    }

    [Fact]
    public async Task ReadAsync_ThrowsWithJsonCause_WhenFileHoldsNull()
    {
        using var service = NewService();
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        await File.WriteAllTextAsync(StatePath, "null");

        var exception = await Assert.ThrowsAsync<ServiceException>(() => service.ReadAsync());

        Assert.IsType<JsonException>(exception.InnerException);
    }

    [Fact]
    public void Read_ThrowsWithJsonCause_WhenFileHoldsNull()
    {
        using var service = NewService();
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, "null");

        var exception = Assert.Throws<ServiceException>(() => service.Read());

        Assert.IsType<JsonException>(exception.InnerException);
    }

    // The state file is in a directory which does not exist yet => force create.
    private string StatePath => Path.Combine(_tempDir.Path, "state", "state.json");

    private DefaultPersistentJsonService<PlaylistSyncState> NewService() => new(StatePath);

    private string[] StagingFiles() => Directory.GetFiles(Path.GetDirectoryName(StatePath)!, "*.tmp");

    private static PlaylistSyncState StateWith(string playlistId)
    {
        var state = new PlaylistSyncState();
        state.Entries.Add(new PlaylistSyncEntry { ListenBrainzPlaylistId = playlistId });
        return state;
    }
}
