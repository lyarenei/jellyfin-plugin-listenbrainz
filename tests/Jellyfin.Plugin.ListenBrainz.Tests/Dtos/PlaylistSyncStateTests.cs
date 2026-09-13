using System;
using System.Linq;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Dtos;

public class PlaylistSyncStateTests
{
    private static DiscoveredPlaylist Discovered(
        string mbid,
        string title = "Weekly Jams",
        DateTime? createdAt = null)
    {
        return new DiscoveredPlaylist(
            mbid,
            PlaylistOrigin.Generated,
            "Jams",
            title,
            createdAt ?? DateTime.UtcNow);
    }

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
        Assert.Equal(createdAt, entry.SyncedCreatedAt);
    }

    [Fact]
    public void ApplyDiscovery_RefreshesMetadataOfKnownPlaylist()
    {
        var state = new PlaylistSyncState();
        var userId = Guid.NewGuid();
        var firstCreatedAt = DateTime.UtcNow.AddDays(-7);
        var regeneratedAt = DateTime.UtcNow;

        var entry = state.ApplyDiscovery(userId, [Discovered("lb-1", createdAt: firstCreatedAt)]).Single();
        PlaylistSyncState.RecordSync(entry, Guid.NewGuid());

        state.ApplyDiscovery(userId, [Discovered("lb-1", title: "Renamed", createdAt: regeneratedAt)]);

        Assert.Equal("Renamed", entry.Title);
        Assert.Equal(regeneratedAt, entry.CreatedAt);

        // The synced version is still the old one, which is what marks the entry for a resync.
        Assert.Equal(firstCreatedAt, entry.SyncedCreatedAt);
    }

    [Fact]
    public void ApplyDiscovery_ScopesEntriesToTheUser()
    {
        var state = new PlaylistSyncState();
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();

        state.ApplyDiscovery(firstUser, [Discovered("lb-1")]);
        state.ApplyDiscovery(secondUser, [Discovered("lb-1")]);

        Assert.Equal(2, state.Entries.Count);
        Assert.Single(state.EntriesFor(firstUser));
        Assert.Single(state.EntriesFor(secondUser));
    }

    [Fact]
    public void ClearSyncResult_MarksEntryAsNeverSynced()
    {
        var entry = new PlaylistSyncEntry { CreatedAt = DateTime.UtcNow };
        PlaylistSyncState.RecordSync(entry, Guid.NewGuid());

        PlaylistSyncState.ClearSyncResult(entry);

        Assert.Null(entry.JellyfinPlaylistId);
        Assert.Null(entry.LastSyncedAt);
        Assert.Null(entry.SyncedCreatedAt);
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
