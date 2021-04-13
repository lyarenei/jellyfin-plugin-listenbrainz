using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    [DataContract]
    public class RecordingIdResponse : BaseResponse
    {
        [DataMember(Name = "recordings")]
        public List<Recording> Recordings { get; set; }

        public override string GetData() => Recordings[0].Id;
    }
}
