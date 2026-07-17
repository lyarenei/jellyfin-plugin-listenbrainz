using System;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Dtos;

public class PlaylistSyncStateTests
{
    [Fact]
    public void Upsert_CreatesThenUpdatesSameMapping()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();
        var firstJfId = Guid.NewGuid();
        var secondJfId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var created = state.Upsert(userId, "lb-1", firstJfId, "Weekly Jams", createdAt, "Jams");
        Assert.Single(state.Mappings);
        Assert.Equal(firstJfId, created.JellyfinPlaylistId);

        var updated = state.Upsert(userId, "lb-1", secondJfId, "Weekly Jams (new)", createdAt, "Jams");

        Assert.Single(state.Mappings);
        Assert.Same(created, updated);
        Assert.Equal(secondJfId, updated.JellyfinPlaylistId);
        Assert.Equal("Weekly Jams (new)", updated.Title);
    }

    [Fact]
    public void FindMapping_MatchesByUserAndId_CaseInsensitive()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();
        state.Upsert(userId, "LB-ABC", Guid.NewGuid(), "title", DateTime.UtcNow, "Jams");

        Assert.NotNull(state.FindMapping(userId, "lb-abc"));
        Assert.Null(state.FindMapping(Guid.NewGuid(), "LB-ABC"));
        Assert.Null(state.FindMapping(userId, "other"));
    }
}
