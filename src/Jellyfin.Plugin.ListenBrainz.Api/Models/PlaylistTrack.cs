using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models;

/// <summary>
/// Playlist track.
/// </summary>
public class PlaylistTrack
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistTrack"/> class.
    /// </summary>
    public PlaylistTrack()
    {
        Identifier = new List<string>();
        Title = string.Empty;
        Creator = string.Empty;
        Album = string.Empty;
        ExtensionData = new Dictionary<string, object>();
        JspfTrack = new JspfTrack();
    }

    /// <summary>
    /// Gets or sets track identifier (MusicBrainz recording URL).
    /// For some reason, this is an array.
    /// </summary>
    public IEnumerable<string> Identifier { get; set; }

    /// <summary>
    /// Gets or sets track title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets track creator (artist name).
    /// </summary>
    public string Creator { get; set; }

    /// <summary>
    /// Gets or sets album name.
    /// </summary>
    public string Album { get; set; }

    /// <summary>
    /// Gets or sets track duration in milliseconds.
    /// </summary>
    public long? Duration { get; set; }

    /// <summary>
    /// Gets or sets JSPF track extension data.
    /// </summary>
    public JspfTrack JspfTrack { get; set; }

    /// <summary>
    /// Gets recording MBID from the identifier URL.
    /// </summary>
    [JsonIgnore]
    public string? RecordingMbid
    {
        get
        {
            // MusicBrainz recording URL is in format
            // https://musicbrainz.org/recording/{mbid}

            var url = Identifier.FirstOrDefault();
            var parts = url?.Split('/');
            return parts?.Length > 1 ? parts[^1] : null;
        }
    }

    /// <summary>
    /// Gets release (album) MBID from JSPF track extension data.
    /// </summary>
    [JsonIgnore]
    public string? ReleaseMbid
    {
        get
        {
            var mbid = JspfTrack.AdditionalMetadata.CaaReleaseMbid;
            return string.IsNullOrEmpty(mbid) ? null : mbid;
        }
    }

    /// <summary>
    /// Gets artist MBIDs from JSPF track extension data.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<string> ArtistMbids
    {
        get
        {
            // MusicBrainz artist URL is in format
            // https://musicbrainz.org/artist/{mbid}

            return JspfTrack.ArtistIdentifiers
                .Select(url =>
                {
                    var parts = url.Split('/');
                    return parts.Length > 1 ? parts[^1] : null;
                })
                .Where(mbid => mbid is not null)!;
        }
    }

    [JsonExtensionData]
    private Dictionary<string, object> ExtensionData { get; set; }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        ExtensionData.TryGetValue("extension", out var extObject);
        if (extObject is null)
        {
            return;
        }

        var serializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
            ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
        };

        var rawJspf = JsonConvert.DeserializeObject<Dictionary<string, object>>(
            extObject.ToString() ?? string.Empty,
            serializerSettings);

        if (rawJspf is null)
        {
            return;
        }

        var jspfKey = "https://musicbrainz.org/doc/jspf#track";
        rawJspf.TryGetValue(jspfKey, out var serializedJspf);
        if (serializedJspf is null)
        {
            return;
        }

        var jspfTrack = JsonConvert.DeserializeObject<JspfTrack>(
            serializedJspf.ToString() ?? string.Empty,
            serializerSettings);

        if (jspfTrack is null)
        {
            return;
        }

        JspfTrack = jspfTrack;
    }
}
