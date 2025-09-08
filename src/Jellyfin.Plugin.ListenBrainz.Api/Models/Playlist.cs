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
        ExtensionData = new Dictionary<string, object>();
        JspfPlaylist = new JspfPlaylist();
        Tracks = new List<object>();
    }

    /// <summary>
    /// Gets or sets annotation.
    /// </summary>
    public required string Annotation { get; set; }

    /// <summary>
    /// Gets or sets creator.
    /// </summary>
    public required string Creator { get; set; }

    /// <summary>
    /// Gets or sets creation date.
    /// </summary>
    [JsonProperty("date")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public required string Identifier { get; set; }

    /// <summary>
    /// Gets or sets title.
    /// </summary>
    public required string Title { get; set; }

    /// <summary>
    /// Gets or sets playlist tracks.
    /// </summary>
    [JsonProperty("track")]
    public IEnumerable<object> Tracks { get; set; }

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

/// <summary>
/// JSPF extension data.
/// </summary>
public class JspfPlaylist
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JspfPlaylist"/> class.
    /// </summary>
    public JspfPlaylist()
    {
        AdditionalMetadata = new JspfAdditionalMetadata();
        Collaborators = new List<string>();
        CreatedFor = string.Empty;
        Creator = string.Empty;
    }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    [JsonProperty("additional_metadata")]
    private JspfAdditionalMetadata AdditionalMetadata { get; set; }

    /// <summary>
    /// Gets or sets playlist collaborators.
    /// </summary>
    public IEnumerable<string> Collaborators { get; set; }

    /// <summary>
    /// Gets or sets name of the user the playlist was created for.
    /// </summary>
    public string CreatedFor { get; set; }

    /// <summary>
    /// Gets or sets name of the playlist creator.
    /// </summary>
    public string Creator { get; set; }

    /// <summary>
    /// Gets or sets last modified date.
    /// </summary>
    public DateTime LastModifiedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is public.
    /// </summary>
    public bool Public { get; set; }
}

internal class JspfAdditionalMetadata
{
    internal JspfAdditionalMetadata()
    {
        AlgorithmMetadata = new JspfAlgorithmMetadata();
    }

    public JspfAlgorithmMetadata AlgorithmMetadata { get; set; }
}

internal class JspfAlgorithmMetadata
{
    internal JspfAlgorithmMetadata()
    {
        SourcePatch = string.Empty;
    }

    public string SourcePatch { get; set; }
}
