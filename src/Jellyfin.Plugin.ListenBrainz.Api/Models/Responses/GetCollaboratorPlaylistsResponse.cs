using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

/// <summary>
/// User listens response.
/// </summary>
public class GetCollaboratorPlaylistsResponse : IListenBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetCollaboratorPlaylistsResponse"/> class.
    /// </summary>
    public GetCollaboratorPlaylistsResponse()
    {
        Playlists = new List<Playlist>();
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
    /// Gets or sets playlists data.
    /// </summary>
    public IEnumerable<Playlist> Playlists { get; set; }
}
