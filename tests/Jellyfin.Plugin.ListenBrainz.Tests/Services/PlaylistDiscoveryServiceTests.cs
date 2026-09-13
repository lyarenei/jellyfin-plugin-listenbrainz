using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public class PlaylistDiscoveryServiceTests
{
    private static Playlist MakePlaylist(string sourcePatch, string mbid, DateTime createdAt, string title = "title")
    {
        return new Playlist
        {
            Identifier = $"https://listenbrainz.org/playlist/{mbid}",
            CreatedAt = createdAt,
            Title = title,
            JspfPlaylist = new JspfPlaylist(sourcePatch),
        };
    }

    private static DefaultPlaylistDiscoveryService ServiceReturning(IEnumerable<Playlist> playlists)
    {
        var listenBrainz = new Mock<IListenBrainzService>();
        listenBrainz
            .Setup(s => s.GetCreatedForPlaylistsAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(playlists);

        return new DefaultPlaylistDiscoveryService(NullLogger.Instance, listenBrainz.Object);
    }

    private static DefaultPlaylistDiscoveryService ServiceThrowing(Exception exception)
    {
        var listenBrainz = new Mock<IListenBrainzService>();
        listenBrainz
            .Setup(s => s.GetCreatedForPlaylistsAsync(
                It.IsAny<UserConfig>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        return new DefaultPlaylistDiscoveryService(NullLogger.Instance, listenBrainz.Object);
    }

    private static UserConfig EnabledForJams()
    {
        return new UserConfig
        {
            UserName = "someone",
            IsWeeklyJamsSyncEnabled = true,
            IsWeeklyExplorationSyncEnabled = false,
            IsTopDiscoveriesSyncEnabled = false,
            IsTopMissedRecordingsSyncEnabled = false,
        };
    }

    [Fact]
    public async Task DiscoverAsync_MapsSelectedPlaylistsToGeneratedEntries()
    {
        var createdAt = DateTime.UtcNow;
        var service = ServiceReturning([MakePlaylist("weekly-jams", "jams-1", createdAt, "Weekly Jams")]);

        var result = await service.DiscoverAsync(EnabledForJams(), CancellationToken.None);

        Assert.True(result.IsComplete);
        var discovered = Assert.Single(result.Playlists);
        Assert.Equal("jams-1", discovered.ListenBrainzPlaylistId);
        Assert.Equal(PlaylistOrigin.Generated, discovered.Origin);
        Assert.Equal("Jams", discovered.GeneratedType);
        Assert.Equal("Weekly Jams", discovered.Title);
        Assert.Equal(createdAt, discovered.CreatedAt);
    }

    [Fact]
    public async Task DiscoverAsync_AppliesUserSettings()
    {
        var service = ServiceReturning(
        [
            MakePlaylist("weekly-jams", "jams-1", DateTime.UtcNow),
            MakePlaylist("weekly-exploration", "expl-1", DateTime.UtcNow),
        ]);

        var result = await service.DiscoverAsync(EnabledForJams(), CancellationToken.None);

        Assert.True(result.IsComplete);
        Assert.Equal(["jams-1"], result.Playlists.Select(p => p.ListenBrainzPlaylistId));
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsIncompleteResult_WhenListingFails()
    {
        // ServiceException is what the ListenBrainz service actually throws, and it does not
        // derive from PluginException - discovery has to survive both.
        var service = ServiceThrowing(new ServiceException("listing failed"));

        var result = await service.DiscoverAsync(EnabledForJams(), CancellationToken.None);

        Assert.False(result.IsComplete);
        Assert.Empty(result.Playlists);
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsIncompleteResult_WhenListingThrowsPluginException()
    {
        var service = ServiceThrowing(new PluginException("listing failed"));

        var result = await service.DiscoverAsync(EnabledForJams(), CancellationToken.None);

        Assert.False(result.IsComplete);
        Assert.Empty(result.Playlists);
    }

    [Fact]
    public async Task DiscoverAsync_ReturnsCompleteEmptyResult_WhenUserHasNoMatchingPlaylists()
    {
        // An empty-but-complete listing is what lets the caller prune; it must not be
        // confused with a failed one.
        var service = ServiceReturning([]);

        var result = await service.DiscoverAsync(EnabledForJams(), CancellationToken.None);

        Assert.True(result.IsComplete);
        Assert.Empty(result.Playlists);
    }
}
