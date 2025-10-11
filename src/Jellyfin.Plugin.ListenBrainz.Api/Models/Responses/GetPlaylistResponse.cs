using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Newtonsoft.Json;

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
        WrappedPlaylist = new();
    }

    /// <inheritdoc />
    public bool IsOk { get; set; }

    /// <inheritdoc />
    public bool IsNotOk => !IsOk;

    /// <summary>
    /// Gets playlist data.
    /// </summary>
    [JsonIgnore]
    public Playlist Playlist => WrappedPlaylist.Playlist;

    [JsonProperty("playlist")]
    private WrappedPlaylist WrappedPlaylist { get; set; }
}
