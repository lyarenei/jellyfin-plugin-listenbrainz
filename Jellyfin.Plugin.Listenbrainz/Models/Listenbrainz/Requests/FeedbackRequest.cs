using System.Text.Json.Serialization;
using Jellyfin.Plugin.Listenbrainz.Resources.ListenBrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    /// <summary>
    /// Request model for user feedback..
    /// </summary>
    public class FeedbackRequest : BaseRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeedbackRequest"/> class.
        /// </summary>
        public FeedbackRequest()
        {
            RecordingMsId = string.Empty;
            RecordingMbid = string.Empty;
        }

        /// <summary>
        /// Gets or sets feedback score.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Gets or sets recording MSID.
        /// </summary>
        [JsonPropertyName("recording_msid")]
        public string? RecordingMsId { get; set; }

        /// <summary>
        /// Gets or sets recording MBID.
        /// </summary>
        [JsonPropertyName("recording_mbid")]
        public string? RecordingMbid { get; set; }

        /// <inheritdoc />
        public override string GetEndpoint() => Endpoints.RecordingFeedback;
    }
}
