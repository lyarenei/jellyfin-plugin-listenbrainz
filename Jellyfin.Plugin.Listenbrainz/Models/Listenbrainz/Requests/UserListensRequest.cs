using System.Collections.Generic;
using static Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    public class UserListensRequest : BaseRequest
    {
        private readonly string UserName;

        public int Count { get; set; } = 5;

        public UserListensRequest(string userName)
        {
            UserName = userName;
        }

        public override Dictionary<string, dynamic> ToRequestForm() => new() { { "count", Count } };

        public override string GetEndpoint() => string.Format(UserEndpoints.ListensEndpoint, UserName);
    }
}
