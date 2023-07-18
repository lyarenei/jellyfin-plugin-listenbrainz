namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Artist credit data.
/// </summary>
public class ArtistCredit
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArtistCredit"/> class.
    /// </summary>
    /// <param name="name">Artist name.</param>
    /// <param name="joinPhrase">Artist join phrase for full credit string.</param>
    public ArtistCredit(string name, string joinPhrase = "")
    {
        Name = name;
        JoinPhrase = joinPhrase;
    }

    /// <summary>
    /// Gets join phrase for this artist.
    /// </summary>
    public string JoinPhrase { get; }

    /// <summary>
    /// Gets artist name.
    /// </summary>
    public string Name { get; }
}
