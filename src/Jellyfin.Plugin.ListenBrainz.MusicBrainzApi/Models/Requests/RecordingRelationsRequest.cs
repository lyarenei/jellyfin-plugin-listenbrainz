using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Resources;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Requests;

/// <summary>
/// Recording relations request.
/// </summary>
public class RecordingRelationsRequest : IMusicBrainzRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingRelationsRequest"/> class.
    /// </summary>
    /// <param name="recordingMbid">Recording MBID.</param>
    public RecordingRelationsRequest(string recordingMbid)
    {
        BaseUrl = Api.BaseUrl;
        RecordingMbid = recordingMbid;
    }

    /// <inheritdoc />
    public string Endpoint => Endpoints.Recording;

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <summary>
    /// Gets recording MBID.
    /// </summary>
    public string RecordingMbid { get; }

    /// <inheritdoc />
    public Dictionary<string, string> SearchQuery => new() { { "inc", "recording-rels" } };
}
