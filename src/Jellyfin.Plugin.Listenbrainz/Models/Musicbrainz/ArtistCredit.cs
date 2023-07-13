using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz
{
    /// <summary>
    /// Artist credit model.
    /// </summary>
    public class ArtistCredit
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ArtistCredit"/> class.
        /// </summary>
        public ArtistCredit()
        {
            JoinPhrase = string.Empty;
            Name = string.Empty;
        }

        /// <summary>
        /// Gets or sets join phrase.
        /// </summary>
        [JsonPropertyName("joinphrase")]
        public string JoinPhrase { get; set; }

        /// <summary>
        /// Gets or sets name.
        /// </summary>
        public string Name { get; set; }
    }
}
