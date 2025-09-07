using System.Runtime.Serialization;
using Newtonsoft.Json;

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
        Extension = new Dictionary<string, object>();
        Identifier = string.Empty;
        Title = string.Empty;
        Source = string.Empty;
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
    /// Gets or sets algorithm source patch.
    /// </summary>
    public string Source { get; set; }

    /// <summary>
    /// Gets or sets extension data.
    /// </summary>
    [JsonExtensionData]
    private Dictionary<string, object> Extension { get; set; }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        var jspfKey = "https://musicbrainz.org/doc/jspf#playlist";
        // SAMAccountName is not deserialized to any property
        // and so it is added to the extension data dictionary
        var jspfData = (JspfData)Extension[jspfKey];
        Source = jspfData.SourcePatch;
    }
}

/// <summary>
/// JSPF playlist data.
/// </summary>
internal class JspfData
{
    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    private Dictionary<string, object> AdditionalMetadata { get; set; }

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

    public string SourcePatch
    {
        get
        {
            try
            {
                var ok = AdditionalMetadata.TryGetValue("algorithm_metadata", out var algoMetadata);
                if (!ok || algoMetadata is null)
                {
                    return string.Empty;
                }

                ok = ((Dictionary<string, object>)algoMetadata).TryGetValue("source_patch", out var source);
                if (!ok || source is null)
                {
                    return string.Empty;
                }

                return (string)source;
            }
            catch (InvalidCastException)
            {
                return string.Empty;
            }
        }
    }
}
