using System.Diagnostics.CodeAnalysis;

namespace Jellyfin.Plugin.Listenbrainz.Configuration
{
    /// <summary>
    /// Global plugin configuration class.
    /// </summary>
    public class GlobalConfiguration
    {
        /// <summary>
        /// Listenbrainz API base URL.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public string? ListenbrainzBaseUrl { get; set; }

        /// <summary>
        /// Musicbrainz API base URL.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public string? MusicbrainzBaseUrl { get; set; }

        /// <summary>
        /// Musicbrainz integration is enabled.
        /// </summary>
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public bool MusicbrainzEnabled { get; set; }
    }
}
