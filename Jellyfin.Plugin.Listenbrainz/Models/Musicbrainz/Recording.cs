using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json.Serialization;

namespace Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz;

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
