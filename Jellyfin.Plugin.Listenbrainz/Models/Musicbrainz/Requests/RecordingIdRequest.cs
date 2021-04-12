using System.Collections.Generic;
using static Jellyfin.Plugin.Listenbrainz.Resources.Musicbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests
{
    public class RecordingIdRequest : BaseRequest
    {
        public string TrackId { get; set; }

        public RecordingIdRequest(string trackId)
        {
            TrackId = trackId;
        }

        public override Dictionary<string, dynamic> ToRequestForm()
        {
            return new Dictionary<string, dynamic> { { "tid", TrackId } };
        }

        public override string GetEndpoint()
        {
            return Endpoints.Recording;
        }
    }
}
