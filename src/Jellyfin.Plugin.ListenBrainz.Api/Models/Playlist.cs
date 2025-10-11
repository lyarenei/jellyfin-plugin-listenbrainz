using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models;

/// <summary>
/// ListenBrainz playlist.
/// </summary>
public class Playlist
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Playlist"/> class.
    /// </summary>
    public Playlist()
    {
        Annotation = string.Empty;
        Creator = string.Empty;
        Identifier = string.Empty;
        Title = string.Empty;
        ExtensionData = new Dictionary<string, object>();
        JspfPlaylist = new JspfPlaylist();
        Tracks = new List<PlaylistTrack>();
    }

    /// <summary>
    /// Gets or sets annotation.
    /// </summary>
    public string Annotation { get; set; }

    /// <summary>
    /// Gets or sets creator.
    /// </summary>
    public string Creator { get; set; }

    /// <summary>
    /// Gets or sets creation date.
    /// </summary>
    [JsonProperty("date")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public string Identifier { get; set; }

    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets playlist tracks.
    /// </summary>
    [JsonProperty("track")]
    public IEnumerable<PlaylistTrack> Tracks { get; set; }

    /// <summary>
    /// Gets or sets JSPF playlist extension data.
    /// </summary>
    public JspfPlaylist JspfPlaylist { get; set; }

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

        var jspfKey = "https://musicbrainz.org/doc/jspf#playlist";
        rawJspf.TryGetValue(jspfKey, out var serializedJspf);
        if (serializedJspf is null)
        {
            return;
        }

        var jspfPlaylist = JsonConvert.DeserializeObject<JspfPlaylist>(
            serializedJspf.ToString() ?? string.Empty,
            serializerSettings);

        if (jspfPlaylist is null)
        {
            return;
        }

        JspfPlaylist = jspfPlaylist;
    }
}
