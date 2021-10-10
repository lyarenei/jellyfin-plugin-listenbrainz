using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses
{
    [DataContract]
    public class BaseResponse
    {
        [DataMember(Name = "message")]
        [JsonPropertyName("message")]
        public string Message { get; set; }

        [DataMember(Name = "error")]
        [JsonPropertyName("error")]
        public string Error { get; set; }

        [DataMember(Name = "status")]
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [DataMember(Name = "code")]
        [JsonPropertyName("code")]
        public int Code { get; set; }

        public virtual bool IsError() => Code > 0 || Status != "ok";
    }
}
