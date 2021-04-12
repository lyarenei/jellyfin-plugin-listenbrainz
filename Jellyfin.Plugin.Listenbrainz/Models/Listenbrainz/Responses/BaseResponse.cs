using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses
{
    [DataContract]
    public class BaseResponse
    {
        [DataMember(Name = "message")]
        public string Message { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "code")]
        public int Code { get; set; }

        public virtual bool IsError() => Code > 0 || Status != "ok";
    }
}
