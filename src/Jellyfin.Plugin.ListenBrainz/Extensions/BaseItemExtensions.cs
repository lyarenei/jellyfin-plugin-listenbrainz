using Jellyfin.Plugin.ListenBrainz.Common;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.ListenBrainz.Extensions;

/// <summary>
/// Extensions for <see cref="BaseItem"/> type.
/// </summary>
public static class BaseItemExtensions
{
    /// <summary>
    /// Convenience method to get a MusicBrainz track ID for this item.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <returns>Track MBID. Null if not available.</returns>
    public static string? GetTrackMbid(this BaseItem item) => item.ProviderIds.GetValueOrDefault("MusicBrainzTrack");

    /// <summary>
    /// Item runtime in seconds.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <returns>Runtime in seconds.</returns>
    public static long RuntimeSeconds(this BaseItem item) => TimeSpan.FromTicks(item.RunTimeTicks ?? 0).Seconds;

    /// <summary>
    /// Create a <see cref="StoredListen"/> from this item.
    /// </summary>
    /// <param name="item">Item data source.</param>
    /// <param name="timestamp">UNIX timestamp of the listen.</param>
    /// <param name="metadata">Additional metadata.</param>
    /// <returns>An instance of <see cref="StoredListen"/> corresponding to the item.</returns>
    public static StoredListen AsStoredListen(this BaseItem item, long timestamp, AudioItemMetadata? metadata)
    {
        return new StoredListen
        {
            Id = item.Id,
            ListenedAt = timestamp,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Create a <see cref="TrackedItem"/> from this item.
    /// </summary>
    /// <param name="item">Item data source.</param>
    /// <param name="userId">Jellyfin user ID associated with this tracked item.</param>
    /// <returns>An instance of <see cref="TrackedItem"/>.</returns>
    public static TrackedItem AsTrackedItem(this BaseItem item, string userId)
    {
        return new TrackedItem
        {
            UserId = userId,
            ItemId = item.Id.ToString(),
            StartedAt = DateUtils.CurrentTimestamp,
            RemoveAfter = DateUtils.CurrentTimestamp + (5 * item.RuntimeSeconds()),
            IsValid = true
        };
    }
}
