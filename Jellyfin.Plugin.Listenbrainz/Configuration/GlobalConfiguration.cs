using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Listenbrainz.Configuration
{
    /// <summary>
    /// Global plugin configuration class.
    /// </summary>
    public class GlobalConfiguration
    {
        /// <summary>
        /// Gets or sets Listenbrainz API base URL.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public string? ListenbrainzBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets Musicbrainz API base URL.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public string? MusicbrainzBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a Musicbrainz integration is enabled.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public bool MusicbrainzEnabled { get; set; }
    }
}
