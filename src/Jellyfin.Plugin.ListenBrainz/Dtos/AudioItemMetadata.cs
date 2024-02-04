using System.Text;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.ListenBrainz.MusicBrainzApi.Models;
using MetaBrainz.MusicBrainz.Interfaces.Entities;

namespace Jellyfin.Plugin.ListenBrainz.Dtos;

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
        RecordingMbid = string.Empty;
        ArtistCredits = new List<ArtistCredit>();
        Isrcs = new List<string>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioItemMetadata"/> class.
    /// </summary>
    /// <param name="recording">MBID recording data.</param>
    public AudioItemMetadata(Recording recording)
    {
        RecordingMbid = recording.Mbid;
        ArtistCredits = recording.ArtistCredits.Select(r => new ArtistCredit(r.Name, r.JoinPhrase));
        Isrcs = recording.Isrcs;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AudioItemMetadata"/> class.
    /// </summary>
    /// <param name="recording">MBID recording data.</param>
    public AudioItemMetadata(IRecording recording)
    {
        RecordingMbid = recording.Id.ToString();
        Isrcs = recording.Isrcs ?? new List<string>();

        var credit = recording.ArtistCredit ?? new List<INameCredit>();
        ArtistCredits = credit.Select(c => new ArtistCredit(c.Name ?? string.Empty, c.JoinPhrase ?? string.Empty));
    }

    /// <summary>
    /// Gets recording MBID for this audio item.
    /// </summary>
    public string RecordingMbid { get; init; }

    /// <summary>
    /// Gets all artists associated with this audio item.
    /// </summary>
    public IEnumerable<ArtistCredit> ArtistCredits { get; init; }

    /// <summary>
    /// Gets full artist credit string using artist names and join phrases.
    /// </summary>
    /// <returns>Artist credit string.</returns>
    [JsonIgnore]
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

    /// <summary>
    /// Gets or sets ISRCs associated with this audio item.
    /// </summary>
    public IEnumerable<string> Isrcs { get; set; }
}
