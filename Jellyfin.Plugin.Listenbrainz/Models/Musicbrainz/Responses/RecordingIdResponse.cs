using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    public class RecordingIdResponse : BaseResponse
    {
        [JsonPropertyName("recordings")]
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
