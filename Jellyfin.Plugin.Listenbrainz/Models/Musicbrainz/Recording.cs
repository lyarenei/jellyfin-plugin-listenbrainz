using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz
{
    public class Recording
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("score")]
        public int Score { get; set; }
    }
}
