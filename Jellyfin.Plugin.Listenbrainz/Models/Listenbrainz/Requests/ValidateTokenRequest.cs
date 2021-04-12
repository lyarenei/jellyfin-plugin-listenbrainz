using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;
using System.Collections.Generic;
using static Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    public class ValidateTokenRequest : BaseRequest
    {
        public string Token { get; set; }

        public ValidateTokenRequest(string token) => Token = token;

        public override Dictionary<string, dynamic> ToRequestForm() => new() { { "token", Token } };

        public override string GetEndpoint() => Endpoints.ValidateToken;
    }
}
