using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses
{
    [DataContract]
    public class UserListensResponse : BaseResponse
    {
        [DataMember(Name = "payload")]
        public UserListensPayload Payload { get; set; }
    }

    [DataContract]
    public class UserListensPayload
    {
        [DataMember(Name = "count")]
        public int Count { get; set; }

        [DataMember(Name = "last_listen_ts")]
        public int LastListenTs { get; set; }

        [DataMember(Name = "listens")]
        public List<Listen> Listens { get; set; }
    }
}
