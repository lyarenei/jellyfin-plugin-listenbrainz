using System.Globalization;
using System.Text;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;

/// <summary>
/// User-owned playlists request.
/// </summary>
public class GetUserPlaylistsRequest : IListenBrainzRequest
{
    private readonly string _userName;
    private readonly CompositeFormat _endpointFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetUserPlaylistsRequest"/> class.
    /// </summary>
    /// <param name="userName">Name of the user who owns the playlists.</param>
    /// <param name="playlistsCount">Number of playlists to fetch.</param>
    /// <param name="offset">Playlist list offset.</param>
    public GetUserPlaylistsRequest(string userName, int playlistsCount = Limits.DefaultItemsPerGet, int offset = 0)
    {
        _endpointFormat = CompositeFormat.Parse(Endpoints.UserPlaylists);
        _userName = userName;
        BaseUrl = General.BaseUrl;
        QueryDict = new Dictionary<string, string>
        {
            { "count", playlistsCount.ToString(NumberFormatInfo.InvariantInfo) },
            { "offset", offset.ToString(NumberFormatInfo.InvariantInfo) }
        };
    }

    /// <inheritdoc />
    public string? ApiToken { get; init; }

    /// <inheritdoc />
    public string Endpoint => string.Format(CultureInfo.InvariantCulture, _endpointFormat, _userName);

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public Dictionary<string, string> QueryDict { get; }
}
