using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using Jellyfin.Plugin.ListenBrainz.Tasks.SyncGeneratedPlaylists;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.WeeklyPlaylists;

public class WeeklyRotationTests
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
    [InlineData("top-discoveries-for-year", "TopDiscoveries")]
    public void Classify_KnownTypes(string sourcePatch, string expectedType)
    {
        Assert.Equal(expectedType, PlaylistTypePolicy.ClassifyBySourcePatch(sourcePatch)?.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("daily-jams")]
    [InlineData("top-recordings-for-year")]
    public void Classify_UnknownType_ReturnsNull(string? sourcePatch)
    {
        Assert.Null(PlaylistTypePolicy.ClassifyBySourcePatch(sourcePatch));
    }

    [Fact]
    public void Selection_KeepsTwoNewestPerFamily()
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
    public void Selection_ExcludesDisabledFamilies()
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
    public void Selection_ArchiveKeepsAllYears()
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

        // Archive types are not capped like rotation types; every year is kept.
        Assert.Equal(3, selected.Count);
        Assert.All(selected, c => Assert.Equal(PlaylistType.TopDiscoveries, c.Type));
    }

    [Fact]
    public void IsUpToDate_SameCreatedAt_ReturnsTrue()
    {
        var createdAt = DateTime.UtcNow;
        var mapping = new PlaylistMapping { ListenBrainzPlaylistId = "jams-1", CreatedAt = createdAt };
        var playlist = MakePlaylist("weekly-jams", "jams-1", createdAt);

        Assert.True(PlaylistTypePolicy.IsUpToDate(mapping, playlist));
    }

    [Fact]
    public void IsUpToDate_DifferentCreatedAt_ReturnsFalse()
    {
        var mapping = new PlaylistMapping { ListenBrainzPlaylistId = "jams-1", CreatedAt = DateTime.UtcNow.AddDays(-7) };
        var playlist = MakePlaylist("weekly-jams", "jams-1", DateTime.UtcNow);

        Assert.False(PlaylistTypePolicy.IsUpToDate(mapping, playlist));
    }

    [Fact]
    public void Prune_OutOfRotationSameFamily_IsPruned()
    {
        var mapping = new PlaylistMapping
        {
            ListenBrainzPlaylistId = "old-jams",
            Category = "Jams",
        };

        var result = PlaylistTypePolicy.ShouldPruneMapping(
            mapping,
            selectedPlaylistIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "current-jams", "previous-jams" },
            syncedTypes: new HashSet<PlaylistType> { PlaylistType.Jams });

        Assert.True(result);
    }

    [Fact]
    public void Prune_StillInRotation_IsKept()
    {
        var mapping = new PlaylistMapping
        {
            ListenBrainzPlaylistId = "current-jams",
            Category = "Jams",
        };

        var result = PlaylistTypePolicy.ShouldPruneMapping(
            mapping,
            selectedPlaylistIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "current-jams", "previous-jams" },
            syncedTypes: new HashSet<PlaylistType> { PlaylistType.Jams });

        Assert.False(result);
    }

    [Fact]
    public void Prune_DisabledFamily_IsKept()
    {
        var mapping = new PlaylistMapping
        {
            ListenBrainzPlaylistId = "old-exploration",
            Category = "Exploration",
        };

        var result = PlaylistTypePolicy.ShouldPruneMapping(
            mapping,
            selectedPlaylistIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "current-jams" },
            syncedTypes: new HashSet<PlaylistType> { PlaylistType.Jams });

        Assert.False(result);
    }

    [Fact]
    public void Prune_ArchiveMapping_IsNeverPruned()
    {
        var mapping = new PlaylistMapping
        {
            ListenBrainzPlaylistId = "disc-2022",
            Category = "TopDiscoveries",
        };

        // The type was synced and this playlist is not among the selected ids, yet archive
        // playlists are permanent and must never be pruned.
        var result = PlaylistTypePolicy.ShouldPruneMapping(
            mapping,
            selectedPlaylistIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "disc-2024" },
            syncedTypes: new HashSet<PlaylistType> { PlaylistType.TopDiscoveries });

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SomeOtherTaskCategory")]
    public void Prune_ForeignOrNullCategory_IsNeverPruned(string? category)
    {
        // The store is shared across sync tasks; the weekly task must leave mappings it does not own.
        var mapping = new PlaylistMapping
        {
            ListenBrainzPlaylistId = "not-a-weekly-playlist",
            Category = category,
        };

        var result = PlaylistTypePolicy.ShouldPruneMapping(
            mapping,
            selectedPlaylistIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "current-jams" },
            syncedTypes: new HashSet<PlaylistType> { PlaylistType.Jams });

        Assert.False(result);
    }
}
