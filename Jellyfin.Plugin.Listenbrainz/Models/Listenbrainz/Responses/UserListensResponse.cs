using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses
{
    public class UserListensResponse : BaseResponse
    {
        [JsonPropertyName("payload")]
        public UserListensPayload Payload { get; set; }

        public override bool IsError() => Error != null;
    }

    public class UserListensPayload
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("latest_listen_ts")]
        public int LastListenTs { get; set; }

        [JsonPropertyName("listens")]
        public List<Listen> Listens { get; set; }
    }
}
