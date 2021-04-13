using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz
{
    [DataContract]
    public class Recording
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "score")]
        public int Score { get; set; }
    }
}
