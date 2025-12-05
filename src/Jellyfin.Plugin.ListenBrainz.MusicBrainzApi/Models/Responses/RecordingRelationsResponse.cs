using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Interfaces;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Requests;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models.Responses;

/// <summary>
/// Response for a <see cref="RecordingRelationsRequest"/>.
/// </summary>
public class RecordingRelationsResponse : IMusicBrainzResponse
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordingRelationsResponse"/> class.
    /// </summary>
    public RecordingRelationsResponse()
    {
        this.Relations = new List<RecordingRelation>();
    }

    /// <summary>
    /// Gets or sets response recordings.
    /// </summary>
    public IEnumerable<RecordingRelation> Relations { get; set; }
}
