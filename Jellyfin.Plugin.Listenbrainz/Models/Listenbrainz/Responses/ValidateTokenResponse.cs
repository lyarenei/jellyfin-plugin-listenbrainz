using System.Runtime.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Responses
{
    public class ValidateTokenResponse : BaseResponse
    {
        [DataMember(Name = "user_name")]
        public string User { get; set; }

        [DataMember(Name = "valid")]
        public bool Valid { get; set; }

        public override bool IsError() => !Valid || Code == 400;
    }
}
