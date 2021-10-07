using System.Collections.Generic;
using System.Runtime.Serialization;
using static Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    [DataContract]
    public class FeedbackRequest : BaseRequest
    {
        public int Score { get; set; }
        public string RecordingMsid { get; set; }

        public override Dictionary<string, dynamic> ToRequestForm()
        {
            return new Dictionary<string, dynamic>
            {
                { "score", Score },
                { "recording_msid", RecordingMsid },
            };
        }

        public override string GetEndpoint() => FeedbackEndpoints.RecordingFeedback;
    }
}
