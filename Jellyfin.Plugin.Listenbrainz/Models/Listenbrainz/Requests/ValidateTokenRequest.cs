using System.Collections.Generic;
using static Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    /// <summary>
    /// Request model for token validation.
    /// </summary>
    public class ValidateTokenRequest : BaseRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidateTokenRequest"/> class.
        /// </summary>
        /// <param name="token">Token to validate.</param>
        public ValidateTokenRequest(string token) => Token = token;

        /// <summary>
        /// Gets token to validate.
        /// </summary>
        private string Token { get; }

        /// <inheritdoc />
        public override object ToRequestForm() => new Dictionary<string, string> { { "token", Token } };

        /// <inheritdoc />
        public override string GetEndpoint() => Endpoints.ValidateToken;
    }
}
