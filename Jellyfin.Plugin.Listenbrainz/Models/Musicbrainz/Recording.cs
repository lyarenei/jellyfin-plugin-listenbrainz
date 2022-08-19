using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz
{
    /// <summary>
    /// Recording model.
    /// </summary>
    public class Recording
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Recording"/> class.
        /// </summary>
        public Recording()
        {
            Id = string.Empty;
            ArtistCredit = new Collection<ArtistCredit>();
        }

        /// <summary>
        /// Gets or sets recording MBID.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets recording match score.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// Gets or sets artist credit of the recording.
        /// </summary>
        [JsonPropertyName("artist-credit")]
        [SuppressMessage("Usage", "CA2227", Justification = "Needed for deserialization.")]
        public Collection<ArtistCredit> ArtistCredit { get; set; }

        /// <summary>
        /// Get full artist credit for the recording.
        /// </summary>
        /// <returns>Artist credit.</returns>
        public string GetCreditString()
        {
            var credit = new StringBuilder();
            foreach (var artistCredit in ArtistCredit)
            {
                credit.Append(artistCredit.Name);
                credit.Append(artistCredit.JoinPhrase);
            }

            return credit.ToString();
        }
    }
}
