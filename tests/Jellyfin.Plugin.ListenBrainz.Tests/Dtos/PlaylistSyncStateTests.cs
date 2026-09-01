using System;
using System.Linq;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Dtos;

public class PlaylistSyncStateTests
{
    [Fact]
    public void Upsert_CreatesThenUpdatesSameEntry()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();
        var firstJfId = Guid.NewGuid();
        var secondJfId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var created = state.Upsert(
            userId, "lb-1", firstJfId, "Weekly Jams", createdAt, PlaylistOrigin.Generated, "Jams");
        Assert.Single(state.Entries);
        Assert.Equal(firstJfId, created.JellyfinPlaylistId);

        var updated = state.Upsert(
            userId, "lb-1", secondJfId, "Weekly Jams (new)", createdAt, PlaylistOrigin.Generated, "Jams");

        Assert.Single(state.Entries);
        Assert.Same(created, updated);
        Assert.Equal(secondJfId, updated.JellyfinPlaylistId);
        Assert.Equal("Weekly Jams (new)", updated.Title);
    }

    [Fact]
    public void Upsert_RecordsOriginAndGeneratedType()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();

        var entry = state.Upsert(
            userId, "lb-1", Guid.NewGuid(), "Weekly Jams", DateTime.UtcNow, PlaylistOrigin.Generated, "Jams");

        Assert.Equal(PlaylistOrigin.Generated, entry.Origin);
        Assert.Equal("Jams", entry.GeneratedType);
    }

    [Fact]
    public void FindEntry_MatchesByUserAndId_CaseInsensitive()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();
        state.Upsert(
            userId, "LB-ABC", Guid.NewGuid(), "title", DateTime.UtcNow, PlaylistOrigin.Generated, "Jams");

        Assert.NotNull(state.FindEntry(userId, "lb-abc"));
        Assert.Null(state.FindEntry(Guid.NewGuid(), "LB-ABC"));
        Assert.Null(state.FindEntry(userId, "other"));
    }

    [Fact]
    public void EntriesFor_ScopesEntriesToTheUser()
    {
        var state = new PlaylistSyncState();
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();

        state.Upsert(
            firstUser, "lb-1", Guid.NewGuid(), "title", DateTime.UtcNow, PlaylistOrigin.Generated, "Jams");
        state.Upsert(
            secondUser, "lb-1", Guid.NewGuid(), "title", DateTime.UtcNow, PlaylistOrigin.Generated, "Jams");

        Assert.Equal(2, state.Entries.Count);
        Assert.Single(state.EntriesFor(firstUser));
        Assert.Equal(secondUser, Assert.Single(state.EntriesFor(secondUser)).JellyfinUserId);
    }

    [Fact]
    public void NewState_CarriesCurrentVersion()
    {
        Assert.Equal(PlaylistSyncState.CurrentVersion, new PlaylistSyncState().Version);
    }
}
