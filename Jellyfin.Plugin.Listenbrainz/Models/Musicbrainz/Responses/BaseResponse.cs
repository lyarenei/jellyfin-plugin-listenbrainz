using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Responses
{
    public class BaseResponse
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }

        public virtual bool IsError() => !string.IsNullOrEmpty(Error);

        public virtual string GetData() => "";
    }
}
