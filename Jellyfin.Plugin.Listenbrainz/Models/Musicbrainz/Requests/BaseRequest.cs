using System.Collections.Generic;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz.Requests
{
    public class BaseRequest
    {
        public BaseRequest() { }

        public virtual Dictionary<string, dynamic> ToRequestForm() => new();

        public virtual string GetEndpoint() => "";
    }
}
