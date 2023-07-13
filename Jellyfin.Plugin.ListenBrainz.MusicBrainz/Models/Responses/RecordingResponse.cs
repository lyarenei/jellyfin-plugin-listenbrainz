using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Dtos;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Requests;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Responses;

/// <summary>
/// Response for a <see cref="RecordingRequest"/>.
/// </summary>
public class RecordingResponse : IMusicBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingResponse"/> class.
    /// </summary>
    public RecordingResponse()
    {
        Recordings = new List<Recording>();
    }

    /// <summary>
    /// Gets or sets response recordings.
    /// </summary>
    public IEnumerable<Recording> Recordings { get; set; }
}
