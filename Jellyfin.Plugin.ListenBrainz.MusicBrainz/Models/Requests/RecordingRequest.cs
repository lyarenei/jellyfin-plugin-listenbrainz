using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Interfaces;
using Jellyfin.Plugin.Listenbrainz.MusicBrainz.Resources;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Requests;

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
        TrackMbid = trackMbid;
    }

    /// <inheritdoc />
    public string Endpoint => Endpoints.Recording;

    /// <summary>
    /// Gets track MBID.
    /// </summary>
    public string TrackMbid { get; }

    /// <inheritdoc />
    public Dictionary<string, string> SearchQuery => new() { { "tid", TrackMbid } };
}
