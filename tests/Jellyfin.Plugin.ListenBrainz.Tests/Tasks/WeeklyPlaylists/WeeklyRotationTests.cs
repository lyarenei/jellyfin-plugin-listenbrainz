using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Tasks;
using Jellyfin.Plugin.ListenBrainz.Tasks.SyncWeeklyPlaylists;
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
    public void Classify_KnownFamilies(string sourcePatch, string expectedType)
    {
        Assert.Equal(expectedType, WeeklyRotationPolicy.ClassifyBySourcePatch(sourcePatch)?.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("daily-jams")]
    [InlineData("top-discoveries-of")]
    public void Classify_NonWeekly_ReturnsNull(string? sourcePatch)
    {
        Assert.Null(WeeklyRotationPolicy.ClassifyBySourcePatch(sourcePatch));
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

        var selected = WeeklyRotationPolicy
            .PickWeeklyRotationPlaylists(playlists, EnabledForBoth())
            .ToList();

        var jamsIds = selected.Where(c => c.Type == WeeklyPlaylistType.Jams)
            .Select(c => c.Playlist.PlaylistId)
            .ToList();

        Assert.Equal(new[] { "jams-3", "jams-2" }, jamsIds);
        Assert.Single(selected, c => c.Type == WeeklyPlaylistType.Exploration);
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

        var selected = WeeklyRotationPolicy
            .PickWeeklyRotationPlaylists(playlists, config)
            .ToList();

        Assert.Single(selected);
        Assert.Equal(WeeklyPlaylistType.Jams, selected[0].Type);
    }

    [Fact]
    public void Prune_OutOfRotationSameFamily_IsPruned()
    {
        var mapping = new PlaylistMapping
        {
            ListenBrainzPlaylistId = "old-jams",
            Category = "Jams",
        };

        var result = WeeklyRotationPolicy.ShouldPruneMapping(
            mapping,
            rotationIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "current-jams", "previous-jams" },
            rotationTypes: new HashSet<WeeklyPlaylistType> { WeeklyPlaylistType.Jams });

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

        var result = WeeklyRotationPolicy.ShouldPruneMapping(
            mapping,
            rotationIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "current-jams", "previous-jams" },
            rotationTypes: new HashSet<WeeklyPlaylistType> { WeeklyPlaylistType.Jams });

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

        var result = WeeklyRotationPolicy.ShouldPruneMapping(
            mapping,
            rotationIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "current-jams" },
            rotationTypes: new HashSet<WeeklyPlaylistType> { WeeklyPlaylistType.Jams });

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

        var result = WeeklyRotationPolicy.ShouldPruneMapping(
            mapping,
            rotationIds: new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "current-jams" },
            rotationTypes: new HashSet<WeeklyPlaylistType> { WeeklyPlaylistType.Jams });

        Assert.False(result);
    }
}
