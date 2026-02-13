using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

/// <summary>
/// Playlist response.
/// </summary>
public class GetPlaylistResponse : IListenBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlaylistResponse"/> class.
    /// </summary>
    public GetPlaylistResponse()
    {
        Playlist = new();
    }

    /// <inheritdoc />
    public bool IsOk { get; set; }

    /// <inheritdoc />
    public bool IsNotOk => !IsOk;

    /// <summary>
    /// Gets or sets playlist data.
    /// </summary>
    public Playlist Playlist { get; set; }
}
