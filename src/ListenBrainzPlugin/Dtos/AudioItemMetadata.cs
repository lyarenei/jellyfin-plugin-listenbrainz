using System.Text;
using ListenBrainzPlugin.MusicBrainzApi.Models;

namespace ListenBrainzPlugin.Dtos;

/// <summary>
/// Additional audio item metadata.
/// </summary>
public class AudioItemMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AudioItemMetadata"/> class.
    /// </summary>
    public AudioItemMetadata()
    {
        Mbid = string.Empty;
        ArtistCredits = Array.Empty<ArtistCredit>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioItemMetadata"/> class.
    /// </summary>
    /// <param name="recording">MBID recording data.</param>
    public AudioItemMetadata(Recording recording)
    {
        Mbid = recording.Mbid;
        ArtistCredits = recording.ArtistCredits.Select(r => new ArtistCredit(r.Name, r.JoinPhrase));
    }

    /// <summary>
    /// Gets recording MBID for this audio item.
    /// </summary>
    public string Mbid { get; init; }

    /// <summary>
    /// Gets all artists associated with this audio item.
    /// </summary>
    public IEnumerable<ArtistCredit> ArtistCredits { get; init; }

    /// <summary>
    /// Gets full artist credit string using artist names and join phrases.
    /// </summary>
    /// <returns>Artist credit string.</returns>
    public string FullCreditString
    {
        get
        {
            var creditString = new StringBuilder();
            foreach (var artistCredit in this.ArtistCredits)
            {
                creditString.Append(artistCredit.Name);
                creditString.Append(artistCredit.JoinPhrase);
            }

            return creditString.ToString();
        }
    }
}
