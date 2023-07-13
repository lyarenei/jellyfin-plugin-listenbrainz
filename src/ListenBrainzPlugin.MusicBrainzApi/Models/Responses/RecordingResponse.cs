using ListenBrainzPlugin.MusicBrainzApi.Interfaces;
using ListenBrainzPlugin.MusicBrainzApi.Models.Requests;

namespace ListenBrainzPlugin.MusicBrainzApi.Models.Responses;

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
        this.Recordings = new List<Recording>();
    }

    /// <summary>
    /// Gets or sets response recordings.
    /// </summary>
    public IEnumerable<Recording> Recordings { get; set; }
}
