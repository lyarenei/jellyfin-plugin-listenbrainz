using System.Globalization;
using System.Text;
using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;

/// <summary>
/// Collaborator playlists request.
/// </summary>
public class GetCollaboratorPlaylistsRequest : IListenBrainzRequest
{
    private readonly string _userName;
    private readonly CompositeFormat _endpointFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetCollaboratorPlaylistsRequest"/> class.
    /// </summary>
    /// <param name="userName">Name of the user (collaborator).</param>
    /// <param name="playlistsCount">Number of playlists to fetch.</param>
    public GetCollaboratorPlaylistsRequest(string userName, int playlistsCount = 10)
    {
        _endpointFormat = CompositeFormat.Parse(Endpoints.CollaboratorPlaylistsEndpoint);
        _userName = userName;
        BaseUrl = General.BaseUrl;
        QueryDict = new Dictionary<string, string> { { "count", playlistsCount.ToString(NumberFormatInfo.InvariantInfo) } };
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
