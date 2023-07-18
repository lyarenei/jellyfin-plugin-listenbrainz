namespace ListenBrainzPlugin.Dtos;

/// <summary>
/// Tracked item.
/// </summary>
public class TrackedItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrackedItem"/> class.
    /// </summary>
    public TrackedItem()
    {
        UserId = string.Empty;
        ItemId = string.Empty;
    }

    /// <summary>
    /// Gets Jellyfin user ID associated with this tracking.
    /// </summary>
    public string UserId { get; init; }

    /// <summary>
    /// Gets item ID this tracking is for.
    /// </summary>
    public string ItemId { get; init; }

    /// <summary>
    /// Gets UNIX timestamp of when the tracking started.
    /// </summary>
    public long StartedAt { get; init; }

    /// <summary>
    /// Gets UNIX timestamp indicating when the tracking for this item can be stopped.
    /// </summary>
    public long RemoveAfter { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether tracking is valid.
    /// Invalid tracked item is effectively a tracking waiting for removal
    /// and should not be taken into account.
    /// </summary>
    public bool IsValid { get; set; }
}
