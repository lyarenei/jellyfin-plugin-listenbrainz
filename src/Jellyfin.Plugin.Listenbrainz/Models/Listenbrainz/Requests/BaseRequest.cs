using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    /// <summary>
    /// Base model for Listenbrainz API requests.
    /// </summary>
    public class BaseRequest
    {
        /// <summary>
        /// Gets or sets API token for authorization.
        /// </summary>
        [JsonIgnore]
        public string? ApiToken { get; set; }

        /// <summary>
        /// Converts request data to a form suitable for use as request data.
        /// </summary>
        /// <returns>Object suitable for use as request data.</returns>
        public virtual object ToRequestForm() => this;

        /// <summary>
        /// Get endpoint name of this request.
        /// </summary>
        /// <returns>Endpoint name.</returns>
        public virtual string GetEndpoint() => string.Empty;
    }
}
