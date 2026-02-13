using System.Globalization;
using System.Text;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Resources;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Requests;

/// <summary>
/// Recording relations request.
/// </summary>
public class RecordingRelationsRequest : IMusicBrainzRequest
{
    private readonly string _recordingMbid;
    private readonly CompositeFormat _endpointFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingRelationsRequest"/> class.
    /// </summary>
    /// <param name="recordingMbid">Recording MBID.</param>
    public RecordingRelationsRequest(string recordingMbid)
    {
        _recordingMbid = recordingMbid;
        _endpointFormat = CompositeFormat.Parse(Endpoints.RecordingData);
        BaseUrl = Api.BaseUrl;
    }

    /// <inheritdoc />
    public string Endpoint => string.Format(CultureInfo.InvariantCulture, _endpointFormat, _recordingMbid);

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public Dictionary<string, string> SearchQuery => new() { { "inc", "recording-rels" } };
}
