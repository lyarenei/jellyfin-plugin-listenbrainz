using System.Collections.Generic;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    public class BaseRequest
    {
        public string ApiToken { get; set; }

        public virtual Dictionary<string, dynamic> ToRequestForm() => new Dictionary<string, dynamic>();

        public virtual string GetEndpoint() => "";
    }
}
