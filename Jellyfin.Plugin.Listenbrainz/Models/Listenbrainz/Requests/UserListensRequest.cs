using System.Collections.Generic;
using static Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    public class UserListensRequest : BaseRequest
    {
        private readonly string UserName;

        private int Count { get; set; }

        public UserListensRequest(string userName, int count = 5)
        {
            UserName = userName;
            Count = count;
        }

        public override Dictionary<string, dynamic> ToRequestForm() => new() { { "count", Count } };

        public override string GetEndpoint() => string.Format(UserEndpoints.ListensEndpoint, UserName);
    }
}
