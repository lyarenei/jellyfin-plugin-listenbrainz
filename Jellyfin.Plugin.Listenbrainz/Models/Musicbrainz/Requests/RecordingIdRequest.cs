using System.Collections.Generic;
using static Jellyfin.Plugin.Listenbrainz.Resources.Musicbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests
{
    /// <summary>
    /// Request model for recording MBID.
    /// </summary>
    public class RecordingIdRequest : BaseRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingIdRequest"/> class.
        /// </summary>
        /// <param name="trackId">Track MBID.</param>
        public RecordingIdRequest(string trackId)
        {
            TrackId = trackId;
        }

        /// <summary>
        /// Gets track MBID.
        /// </summary>
        private string TrackId { get; }

        /// <inheritdoc />
        public override Dictionary<string, string> ToRequestForm() => new() { { "tid", TrackId } };

        /// <inheritdoc />
        public override string GetEndpoint() => Endpoints.Recording;
    }
}
