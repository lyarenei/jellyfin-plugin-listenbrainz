using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using Jellyfin.Plugin.ListenBrainz.Tasks.SyncGeneratedPlaylists;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.GeneratedPlaylists;

public class GeneratedPlaylistsTests
{
    private static Playlist MakePlaylist(string sourcePatch, string mbid, DateTime createdAt)
    {
        return new Playlist
        {
            Identifier = $"https://listenbrainz.org/playlist/{mbid}",
            CreatedAt = createdAt,
            JspfPlaylist = new JspfPlaylist(sourcePatch),
        };
    }

    private static UserConfig EnabledForBoth()
    {
        return new UserConfig { IsWeeklyJamsSyncEnabled = true, IsWeeklyExplorationSyncEnabled = true };
    }

    [Theory]
    [InlineData("weekly-jams", "Jams")]
    [InlineData("weekly-exploration", "Exploration")]
    [InlineData("top-discoveries-of-2024", "TopDiscoveries")]
    [InlineData("top-missed-recordings-of-2024", "TopMissedRecordings")]
    public void Classify_KnownTypes(string sourcePatch, string expectedType)
    {
        Assert.Equal(expectedType, PlaylistTypePolicy.ClassifyBySourcePatch(sourcePatch)?.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("daily-jams")]
    [InlineData("top-recordings-for-year")]
    [InlineData("top-discoveries-for-year")]
    public void Classify_UnknownType_ReturnsNull(string? sourcePatch)
    {
        Assert.Null(PlaylistTypePolicy.ClassifyBySourcePatch(sourcePatch));
    }

    [Fact]
    public void Selection_KeepsTwoNewestPerType()
    {
        var now = DateTime.UtcNow;
        var playlists = new[]
        {
            MakePlaylist("weekly-jams", "jams-1", now.AddDays(-21)),
            MakePlaylist("weekly-jams", "jams-2", now.AddDays(-14)),
            MakePlaylist("weekly-jams", "jams-3", now.AddDays(-7)),
            MakePlaylist("weekly-exploration", "expl-1", now.AddDays(-7)),
        };

        var selected = PlaylistTypePolicy
            .SelectPlaylists(playlists, EnabledForBoth())
            .ToList();

        var jamsIds = selected.Where(c => c.Type == PlaylistType.Jams)
            .Select(c => c.Playlist.PlaylistId)
            .ToList();

        Assert.Equal(new[] { "jams-3", "jams-2" }, jamsIds);
        Assert.Single(selected, c => c.Type == PlaylistType.Exploration);
    }

    [Fact]
    public void Selection_ExcludesDisabledTypes()
    {
        var now = DateTime.UtcNow;
        var playlists = new[]
        {
            MakePlaylist("weekly-jams", "jams-1", now.AddDays(-7)),
            MakePlaylist("weekly-exploration", "expl-1", now.AddDays(-7)),
        };

        var config = new UserConfig { IsWeeklyJamsSyncEnabled = true, IsWeeklyExplorationSyncEnabled = false };

        var selected = PlaylistTypePolicy
            .SelectPlaylists(playlists, config)
            .ToList();

        Assert.Single(selected);
        Assert.Equal(PlaylistType.Jams, selected[0].Type);
    }

    [Fact]
    public void Selection_UncappedTypeKeepsAllPlaylists()
    {
        var now = DateTime.UtcNow;
        var playlists = new[]
        {
            MakePlaylist("top-discoveries-of-2022", "disc-2022", now.AddYears(-2)),
            MakePlaylist("top-discoveries-of-2023", "disc-2023", now.AddYears(-1)),
            MakePlaylist("top-discoveries-of-2024", "disc-2024", now),
        };

        var selected = PlaylistTypePolicy
            .SelectPlaylists(playlists, new UserConfig())
            .ToList();

        Assert.Equal(3, selected.Count);
        Assert.All(selected, c => Assert.Equal(PlaylistType.TopDiscoveries, c.Type));
    }

    private static PlaylistSyncEntry MakeEntry(
        string mbid,
        string? generatedType,
        PlaylistOrigin origin = PlaylistOrigin.Generated)
    {
        return new PlaylistSyncEntry
        {
            ListenBrainzPlaylistId = mbid,
            Origin = origin,
            GeneratedType = generatedType,
        };
    }

    private static HashSet<string> Selected(params string[] ids)
    {
        return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void IsUpToDate_SyncedCurrentVersion_ReturnsTrue()
    {
        var createdAt = DateTime.UtcNow;
        var entry = new PlaylistSyncEntry
        {
            ListenBrainzPlaylistId = "jams-1",
            CreatedAt = createdAt,
            SyncedCreatedAt = createdAt,
        };

        Assert.True(PlaylistTypePolicy.IsUpToDate(entry));
    }

    [Fact]
    public void IsUpToDate_PlaylistRegeneratedSinceSync_ReturnsFalse()
    {
        var entry = new PlaylistSyncEntry
        {
            ListenBrainzPlaylistId = "jams-1",
            CreatedAt = DateTime.UtcNow,
            SyncedCreatedAt = DateTime.UtcNow.AddDays(-7),
        };

        Assert.False(PlaylistTypePolicy.IsUpToDate(entry));
    }

    [Fact]
    public void IsUpToDate_NeverSynced_ReturnsFalse()
    {
        var entry = new PlaylistSyncEntry { ListenBrainzPlaylistId = "jams-1", CreatedAt = DateTime.UtcNow };

        Assert.False(PlaylistTypePolicy.IsUpToDate(entry));
    }

    [Fact]
    public void Prune_OutOfRotationSameType_IsPruned()
    {
        var result = PlaylistTypePolicy.ShouldPruneEntry(
            MakeEntry("old-jams", "Jams"),
            selectedPlaylistIds: Selected("current-jams", "previous-jams"),
            syncedTypes: [PlaylistType.Jams]);

        Assert.True(result);
    }

    [Fact]
    public void Prune_StillInRotation_IsKept()
    {
        var result = PlaylistTypePolicy.ShouldPruneEntry(
            MakeEntry("current-jams", "Jams"),
            selectedPlaylistIds: Selected("current-jams", "previous-jams"),
            syncedTypes: [PlaylistType.Jams]);

        Assert.False(result);
    }

    [Fact]
    public void Prune_TypeThatFailedToSync_IsKept()
    {
        // Pruning the previous playlist would leave the user with nothing for this type.
        var result = PlaylistTypePolicy.ShouldPruneEntry(
            MakeEntry("old-jams", "Jams"),
            selectedPlaylistIds: Selected("current-jams"),
            syncedTypes: []);

        Assert.False(result);
    }

    [Fact]
    public void Prune_DisabledType_IsKept()
    {
        var result = PlaylistTypePolicy.ShouldPruneEntry(
            MakeEntry("old-exploration", "Exploration"),
            selectedPlaylistIds: Selected("current-jams"),
            syncedTypes: [PlaylistType.Jams]);

        Assert.False(result);
    }

    [Fact]
    public void Prune_UncappedType_IsNeverPruned()
    {
        var result = PlaylistTypePolicy.ShouldPruneEntry(
            MakeEntry("disc-2022", "TopDiscoveries"),
            selectedPlaylistIds: Selected("disc-2024"),
            syncedTypes: [PlaylistType.TopDiscoveries]);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SomeOtherTaskCategory")]
    public void Prune_ForeignOrNullType_IsNeverPruned(string? generatedType)
    {
        // The state is shared across sync tasks; entries of other tasks must be left alone.
        var result = PlaylistTypePolicy.ShouldPruneEntry(
            MakeEntry("not-a-generated-playlist", generatedType),
            selectedPlaylistIds: Selected("current-jams"),
            syncedTypes: [PlaylistType.Jams]);

        Assert.False(result);
    }

    [Theory]
    [InlineData(PlaylistOrigin.Unknown)]
    [InlineData(PlaylistOrigin.UserCreated)]
    [InlineData(PlaylistOrigin.Collaborative)]
    public void Prune_NonGeneratedOrigin_IsNeverPruned(PlaylistOrigin origin)
    {
        var result = PlaylistTypePolicy.ShouldPruneEntry(
            MakeEntry("user-playlist", "Jams", origin),
            selectedPlaylistIds: Selected("current-jams"),
            syncedTypes: [PlaylistType.Jams]);

        Assert.False(result);
    }

    [Fact]
    public void ParsePlaylistType_RoundTripsCategoryFor()
    {
        foreach (var type in Enum.GetValues<PlaylistType>())
        {
            Assert.Equal(type, PlaylistTypePolicy.ParsePlaylistType(PlaylistTypePolicy.CategoryFor(type)));
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NotAType")]
    public void ParsePlaylistType_UnknownDiscriminator_ReturnsNull(string? generatedType)
    {
        Assert.Null(PlaylistTypePolicy.ParsePlaylistType(generatedType));
    }
}
