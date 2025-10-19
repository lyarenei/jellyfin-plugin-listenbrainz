using System.Globalization;
using System.Text;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;

/// <summary>
/// Playlist request.
/// </summary>
public class GetPlaylistRequest : IListenBrainzRequest
{
    private readonly string _playlistId;
    private readonly CompositeFormat _endpointFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlaylistRequest"/> class.
    /// </summary>
    /// <param name="playlistId">ListenBrainz playlist ID (MBID).</param>
    /// <param name="fetchMetadata">Fetch additional metadata for tracks in the playlist.</param>
    public GetPlaylistRequest(string playlistId, bool fetchMetadata = true)
    {
        _playlistId = playlistId;
        _endpointFormat = CompositeFormat.Parse(Endpoints.PlaylistDetails);
        BaseUrl = General.BaseUrl;
        QueryDict = new() { { "fetchMetadata", fetchMetadata.ToString(CultureInfo.InvariantCulture) } };
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => string.Format(CultureInfo.InvariantCulture, _endpointFormat, _playlistId);

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public Dictionary<string, string> QueryDict { get; }
}
