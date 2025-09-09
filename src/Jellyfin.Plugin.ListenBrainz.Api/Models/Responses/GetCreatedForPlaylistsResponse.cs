using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

/// <summary>
/// 'Created for' playlists response.
/// </summary>
public class GetCreatedForPlaylistsResponse : IListenBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCreatedForPlaylistsResponse"/> class.
    /// </summary>
    public GetCreatedForPlaylistsResponse()
    {
        WrappedPlaylists = new List<WrappedPlaylist>();
    }

    /// <inheritdoc />
    public bool IsOk { get; set; }

    /// <inheritdoc />
    public bool IsNotOk => !IsOk;

    /// <summary>
    /// Gets or sets requested count of playlists (page size).
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Gets or sets paging offset (see <see cref="Count"/>).
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Gets or sets total playlist count.
    /// </summary>
    public int PlaylistCount { get; set; }

    /// <summary>
    /// Gets playlists data.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<Playlist> Playlists => WrappedPlaylists.Select(wp => wp.Playlist);

    [JsonProperty("playlists")]
    private IEnumerable<WrappedPlaylist> WrappedPlaylists { get; set; }
}

internal class WrappedPlaylist
{
    public required Playlist Playlist { get; set; }
}
