using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Resources;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Requests;

/// <summary>
/// Recording request.
/// </summary>
public class RecordingRequest : IMusicBrainzRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingRequest"/> class.
    /// </summary>
    /// <param name="trackMbid">Track MBID.</param>
    public RecordingRequest(string trackMbid)
    {
        BaseUrl = Api.BaseUrl;
        TrackMbid = trackMbid;
    }

    /// <inheritdoc />
    public string Endpoint => Endpoints.Recording;

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <summary>
    /// Gets track MBID.
    /// </summary>
    public string TrackMbid { get; }

    /// <inheritdoc />
    public Dictionary<string, string> SearchQuery => new() { { "tid", this.TrackMbid } };
}
