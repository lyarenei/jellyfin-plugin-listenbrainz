using System;
using System.Linq;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Dtos;

public class PlaylistSyncStateTests
{
    [Fact]
    public void UpsertDiscovered_CreatesThenRefreshesSameEntry()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var created = state.UpsertDiscovered(
            userId, "lb-1", PlaylistOrigin.Generated, "Jams", "Weekly Jams", createdAt);
        Assert.Single(state.Entries);

        var updated = state.UpsertDiscovered(
            userId, "lb-1", PlaylistOrigin.Generated, "Jams", "Weekly Jams (new)", createdAt);

        Assert.Single(state.Entries);
        Assert.Same(created, updated);
        Assert.Equal("Weekly Jams (new)", updated.Title);
    }

    [Fact]
    public void UpsertDiscovered_RecordsOriginAndGeneratedType()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();

        var entry = state.UpsertDiscovered(
            userId, "lb-1", PlaylistOrigin.Generated, "Jams", "Weekly Jams", DateTime.UtcNow);

        Assert.Equal(PlaylistOrigin.Generated, entry.Origin);
        Assert.Equal("Jams", entry.GeneratedType);
    }

    [Fact]
    public void UpsertDiscovered_KeepsSyncResultOfKnownPlaylist()
    {
        // A new discovery pass must not look like the playlist has never been synced,
        // that would force a resync of everything on every run.
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();
        var jellyfinPlaylistId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var entry = state.UpsertDiscovered(
            userId, "lb-1", PlaylistOrigin.Generated, "Jams", "Weekly Jams", createdAt);
        PlaylistSyncState.RecordSync(entry, jellyfinPlaylistId);

        state.UpsertDiscovered(
            userId, "lb-1", PlaylistOrigin.Generated, "Jams", "Weekly Jams", createdAt);

        Assert.Equal(jellyfinPlaylistId, entry.JellyfinPlaylistId);
        Assert.NotNull(entry.LastSyncedAt);
    }

    [Fact]
    public void ClearSyncResult_MarksEntryAsNeverSynced()
    {
        var entry = new PlaylistSyncEntry { CreatedAt = DateTime.UtcNow };
        PlaylistSyncState.RecordSync(entry, Guid.NewGuid());

        PlaylistSyncState.ClearSyncResult(entry);

        Assert.Null(entry.JellyfinPlaylistId);
        Assert.Null(entry.LastSyncedAt);
    }

    [Fact]
    public void FindEntry_MatchesByUserAndId_CaseInsensitive()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();
        state.UpsertDiscovered(
            userId, "LB-ABC", PlaylistOrigin.Generated, "Jams", "title", DateTime.UtcNow);

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

        state.UpsertDiscovered(
            firstUser, "lb-1", PlaylistOrigin.Generated, "Jams", "title", DateTime.UtcNow);
        state.UpsertDiscovered(
            secondUser, "lb-1", PlaylistOrigin.Generated, "Jams", "title", DateTime.UtcNow);

        Assert.Equal(2, state.Entries.Count);
        Assert.Single(state.EntriesFor(firstUser));
        Assert.Equal(secondUser, Assert.Single(state.EntriesFor(secondUser)).JellyfinUserId);
    }

    [Fact]
    public void NewState_CarriesNoVersion()
    {
        Assert.Equal(0, new PlaylistSyncState().Version);
    }
}
