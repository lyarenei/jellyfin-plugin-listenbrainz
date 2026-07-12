using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Api;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Services;

public class DefaultListenBrainzServiceTests
{
    private readonly Mock<IListenBrainzApiClient> _apiClientMock;
    private readonly Mock<IPluginConfigService> _pluginConfigMock;
    private readonly DefaultListenBrainzService _service;

    public DefaultListenBrainzServiceTests()
    {
        _apiClientMock = new Mock<IListenBrainzApiClient>();
        _pluginConfigMock = new Mock<IPluginConfigService>();
        _pluginConfigMock.SetupGet(pc => pc.ListenBrainzApiUrl).Returns("https://api.listenbrainz.org/1/");

        _service = new DefaultListenBrainzService(
            NullLogger.Instance,
            _apiClientMock.Object,
            _pluginConfigMock.Object);
    }

    private static UserConfig GetUserConfig() => new()
    {
        JellyfinUserId = Guid.NewGuid(),
        UserName = "foobar",
        ApiToken = "token",
        PlaintextApiToken = "token",
    };

    private static GetUserPlaylistsResponse BuildResponse(int count, int offset, int playlistCount, params string[] playlistIds)
    {
        var playlists = string.Join(
            ',',
            playlistIds.Select(id => $"{{\"playlist\":{{\"identifier\":\"https://listenbrainz.org/playlist/{id}\",\"title\":\"{id}\"}}}}"));
        var json = $"{{\"count\":{count},\"offset\":{offset},\"playlist_count\":{playlistCount},\"playlists\":[{playlists}]}}";

        var response = JsonConvert.DeserializeObject<GetUserPlaylistsResponse>(json, BaseApiClient.SerializerSettings)!;
        response.IsOk = true;
        return response;
    }

    [Fact]
    public async Task GetUserPlaylistsAsync_ReturnsAllPlaylists_AcrossMultiplePages()
    {
        _apiClientMock
            .SetupSequence(c => c.GetUserPlaylists(It.IsAny<GetUserPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponse(1, 0, 2, "playlist-a"))
            .ReturnsAsync(BuildResponse(1, 1, 2, "playlist-b"));

        var result = (await _service.GetUserPlaylistsAsync(GetUserConfig(), 1, CancellationToken.None)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("playlist-a", result[0].PlaylistId);
        Assert.Equal("playlist-b", result[1].PlaylistId);
        _apiClientMock.Verify(
            c => c.GetUserPlaylists(It.IsAny<GetUserPlaylistsRequest>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GetUserPlaylistsAsync_ForwardsUserNameApiTokenAndBaseUrl()
    {
        _apiClientMock
            .Setup(c => c.GetUserPlaylists(It.IsAny<GetUserPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResponse(0, 0, 0));

        await _service.GetUserPlaylistsAsync(GetUserConfig(), 25, CancellationToken.None);

        _apiClientMock.Verify(
            c => c.GetUserPlaylists(
                It.Is<GetUserPlaylistsRequest>(r =>
                    r.Endpoint == "user/foobar/playlists"
                    && r.ApiToken == "token"
                    && r.BaseUrl == "https://api.listenbrainz.org/1/"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserPlaylistsAsync_Throws_WhenResponseIsNotOk()
    {
        var response = BuildResponse(0, 0, 0);
        response.IsOk = false;
        _apiClientMock
            .Setup(c => c.GetUserPlaylists(It.IsAny<GetUserPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        await Assert.ThrowsAsync<ServiceException>(() =>
            _service.GetUserPlaylistsAsync(GetUserConfig(), 25, CancellationToken.None));
    }

    [Fact]
    public async Task GetUserPlaylistsAsync_WrapsUnderlyingException()
    {
        var inner = new InvalidOperationException("boom");
        _apiClientMock
            .Setup(c => c.GetUserPlaylists(It.IsAny<GetUserPlaylistsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(inner);

        var ex = await Assert.ThrowsAsync<ServiceException>(() =>
            _service.GetUserPlaylistsAsync(GetUserConfig(), 25, CancellationToken.None));

        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public async Task GetUserPlaylistsAsync_ThrowsWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.GetUserPlaylistsAsync(GetUserConfig(), 25, cts.Token));

        _apiClientMock.Verify(
            c => c.GetUserPlaylists(It.IsAny<GetUserPlaylistsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
