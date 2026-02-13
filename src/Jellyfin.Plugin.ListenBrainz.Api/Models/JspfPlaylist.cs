using Newtonsoft.Json;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models;

/// <summary>
/// JSPF playlist extension data.
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

    /// <summary>
    /// Gets a playlist's source patch.
    /// </summary>
    public string SourcePatch { get => AdditionalMetadata.AlgorithmMetadata.SourcePatch; }
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
