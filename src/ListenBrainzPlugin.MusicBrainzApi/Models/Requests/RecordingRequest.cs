using ListenBrainzPlugin.MusicBrainzApi.Interfaces;
using ListenBrainzPlugin.MusicBrainzApi.Resources;

namespace ListenBrainzPlugin.MusicBrainzApi.Models.Requests;

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
        this.TrackMbid = trackMbid;
    }

    /// <inheritdoc />
    public string Endpoint => Endpoints.Recording;

    /// <summary>
    /// Gets track MBID.
    /// </summary>
    public string TrackMbid { get; }

    /// <inheritdoc />
    public Dictionary<string, string> SearchQuery => new() { { "tid", this.TrackMbid } };
}
