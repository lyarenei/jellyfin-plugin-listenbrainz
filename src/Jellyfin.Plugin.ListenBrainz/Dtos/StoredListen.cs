using MediaBrowser.Controller.Entities.Audio;
using NewtonsoftJson = Newtonsoft.Json;
using SystemJson = System.Text.Json.Serialization;

namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Stored listen of a Jellyfin <see cref="Audio"/> item.
/// </summary>
public class StoredListen
{
    /// <summary>
    /// Gets or sets ID of an <see cref="Audio"/> item associated with this listen.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets UNIX timestamp when this listen has been created.
    /// </summary>
    public long ListenedAt { get; set; }

    /// <summary>
    /// Gets or sets additional metadata for <see cref="Audio"/> item.
    /// </summary>
    public AudioItemMetadata? Metadata { get; set; }

    /// <summary>
    /// Gets a value indicating whether this listen has MusicBrainz recording ID.
    /// </summary>
    [NewtonsoftJson.JsonIgnore]
    [SystemJson.JsonIgnore]
    public bool HasRecordingMbid => !string.IsNullOrEmpty(Metadata?.RecordingMbid);
}
