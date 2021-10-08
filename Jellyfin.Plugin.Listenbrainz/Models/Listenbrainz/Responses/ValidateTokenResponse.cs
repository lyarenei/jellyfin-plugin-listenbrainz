using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses
{
    public class ValidateTokenResponse : BaseResponse
    {
        [JsonPropertyName("user_name")]
        public string Name { get; set; }

        [JsonPropertyName("valid")]
        public bool Valid { get; set; }

        public override bool IsError() => !Valid || Code == 400;
    }
}
