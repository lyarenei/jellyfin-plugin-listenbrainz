using System.Text;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Dtos;

/// <summary>
/// MusicBrainz recording.
/// </summary>
public class Recording
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Recording"/> class.
    /// </summary>
    public Recording()
    {
        Mbid = string.Empty;
        ArtistCredits = new List<ArtistCredit>();
    }

    /// <summary>
    /// Gets or sets recording MBID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Mbid { get; set; }

    /// <summary>
    /// Gets or sets search match score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Gets or sets artist credits.
    /// </summary>
    [JsonPropertyName("artist-credit")]
    public IEnumerable<ArtistCredit> ArtistCredits { get; set; }

    /// <summary>
    /// Constructs full artist credit string.
    /// </summary>
    /// <returns>Artist credit string.</returns>
    public string GetFullCreditString()
    {
        var creditString = new StringBuilder();
        foreach (var artistCredit in ArtistCredits)
        {
            creditString.Append(artistCredit.Name);
            creditString.Append(artistCredit.JoinPhrase);
        }

        return creditString.ToString();
    }
}
