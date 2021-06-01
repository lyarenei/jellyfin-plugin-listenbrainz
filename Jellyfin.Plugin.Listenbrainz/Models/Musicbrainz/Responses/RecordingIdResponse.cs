using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    [DataContract]
    public class RecordingIdResponse : BaseResponse
    {
        [DataMember(Name = "recordings")]
        public List<Recording> Recordings { get; set; }

        public override string GetData()
        {
            try
            {
                return Recordings[0].Id;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }
    }
}
