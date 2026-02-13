using Newtonsoft.Json;

namespace Jellyfin.Plugin.ListenBrainz.Api.Models;

/// <summary>
/// JSPF track extension data.
/// </summary>
public class JspfTrack
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JspfTrack"/> class.
    /// </summary>
    public JspfTrack()
    {
        ArtistIdentifiers = new List<string>();
        AdditionalMetadata = new JspfTrackAdditionalMetadata();
    }

    /// <summary>
    /// Gets or sets artist identifiers (MusicBrainz artist URLs).
    /// </summary>
    public IEnumerable<string> ArtistIdentifiers { get; set; }

    /// <summary>
    /// Gets or sets additional metadata.
    /// </summary>
    [JsonProperty("additional_metadata")]
    public JspfTrackAdditionalMetadata AdditionalMetadata { get; set; }
}

/// <summary>
/// JSPF track additional metadata.
/// </summary>
public class JspfTrackAdditionalMetadata
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JspfTrackAdditionalMetadata"/> class.
    /// </summary>
    public JspfTrackAdditionalMetadata()
    {
        CaaReleaseMbid = string.Empty;
    }

    /// <summary>
    /// Gets or sets Cover Art Archive release MBID.
    /// This corresponds to the MusicBrainz release (album) MBID.
    /// </summary>
    public string CaaReleaseMbid { get; set; }
}
