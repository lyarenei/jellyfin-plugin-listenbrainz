using System.IO;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Services;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public class PersistentJsonServiceTests
{
    private static string NewTempPath()
    {
        return Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "state.json");
    }

    private static PlaylistSyncState StateWith(string playlistId)
    {
        var state = new PlaylistSyncState();
        state.Mappings.Add(new PlaylistMapping { ListenBrainzPlaylistId = playlistId });
        return state;
    }

    [Fact]
    public async Task SaveAsync_ThenReadAsync_RoundTrips()
    {
        var path = NewTempPath();
        try
        {
            using var service = new DefaultPersistentJsonService<PlaylistSyncState>(path);

            await service.SaveAsync(StateWith("mbid"));
            var restored = await service.ReadAsync();

            Assert.Equal("mbid", Assert.Single(restored.Mappings).ListenBrainzPlaylistId);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTemporaryFileBehind()
    {
        // The save stages into a temp file and moves it into place, so the target file is never
        // left truncated. The staging file must not survive a successful save.
        var path = NewTempPath();
        try
        {
            using var service = new DefaultPersistentJsonService<PlaylistSyncState>(path);

            await service.SaveAsync(new PlaylistSyncState());

            Assert.True(File.Exists(path));
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        var path = NewTempPath();
        try
        {
            using var service = new DefaultPersistentJsonService<PlaylistSyncState>(path);

            await service.SaveAsync(StateWith("old"));
            await service.SaveAsync(StateWith("new"));
            var restored = await service.ReadAsync();

            Assert.Equal("new", Assert.Single(restored.Mappings).ListenBrainzPlaylistId);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public void Save_ThenRead_RoundTrips()
    {
        var path = NewTempPath();
        try
        {
            using var service = new DefaultPersistentJsonService<PlaylistSyncState>(path);

            service.Save(StateWith("mbid"));
            var restored = service.Read();

            Assert.Equal("mbid", Assert.Single(restored.Mappings).ListenBrainzPlaylistId);
            Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(path)!, "*.tmp"));
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_Throws_WhenFileIsMissing()
    {
        using var service = new DefaultPersistentJsonService<PlaylistSyncState>(NewTempPath());

        await Assert.ThrowsAsync<ServiceException>(() => service.ReadAsync());
    }
}
