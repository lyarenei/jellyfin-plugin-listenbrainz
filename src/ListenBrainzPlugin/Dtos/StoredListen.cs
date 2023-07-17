using MediaBrowser.Controller.Entities.Audio;

namespace ListenBrainzPlugin.Dtos;

/// <summary>
/// Stored listen of a Jellyfin <see cref="Audio"/> item.
/// </summary>
public class StoredListen
{
    /// <summary>
    /// Gets or sets ID of an item associated with this listen.
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
}
