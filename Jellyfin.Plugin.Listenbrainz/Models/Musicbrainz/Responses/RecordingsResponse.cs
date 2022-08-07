using System.Collections.ObjectModel;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    /// <summary>
    /// Response model for recording MBID.
    /// </summary>
    public class RecordingsResponse : BaseResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingsResponse"/> class.
        /// </summary>
        public RecordingsResponse()
        {
            Recordings = new Collection<Recording>();
        }

        /// <summary>
        /// Gets or sets a collection of recordings.
        /// </summary>
        public Collection<Recording> Recordings { get; set; }
    }
}
