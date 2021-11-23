using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    public class BaseResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; }

        public virtual bool IsError() => !string.IsNullOrEmpty(Error);

        public virtual string GetData() => "";
    }
}
