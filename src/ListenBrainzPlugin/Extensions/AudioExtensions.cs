using ListenBrainzPlugin.Dtos;
using ListenBrainzPlugin.ListenBrainzApi.Models;
using ListenBrainzPlugin.Utils;
using MediaBrowser.Controller.Entities.Audio;

namespace ListenBrainzPlugin.Extensions;

/// <summary>
/// Extensions for <see cref="Audio"/> type.
/// </summary>
public static class AudioExtensions
{
    /// <summary>
    /// Assert this item has required metadata for ListenBrainz submission.
    /// </summary>
    /// <param name="item">Audio item.</param>
    public static void AssertHasMetadata(this Audio item)
    {
        var artistNames = item.Artists.TakeWhile(name => !string.IsNullOrEmpty(name));
        if (!artistNames.Any()) throw new ArgumentException("Item has no valid artists");

        if (string.IsNullOrWhiteSpace(item.Name)) throw new ArgumentException("Item name is empty");
    }

    /// <summary>
    /// Transforms an <see cref="Audio"/> item to a <see cref="Listen"/>.
    /// </summary>
    /// <param name="item">Item to transform.</param>
    /// <param name="timestamp">Timestamp of the listen.</param>
    /// <param name="itemMetadata">Additional item metadata.</param>
    /// <returns>Listen instance with data from the item.</returns>
    public static Listen AsListen(this Audio item, long? timestamp = null, AudioItemMetadata? itemMetadata = null)
    {
        string allArtists = string.Join(", ", item.Artists.TakeWhile(name => !string.IsNullOrEmpty(name)));
        return new Listen
        {
            ListenedAt = timestamp,
            TrackMetadata = new TrackMetadata
            {
                ArtistName = itemMetadata?.FullCreditString ?? allArtists,
                ReleaseName = item.Album,
                TrackName = item.Name,
                AdditionalInfo = new AdditionalInfo
                {
                    MediaPlayer = "Jellyfin",
                    MediaPlayerVersion = null,
                    SubmissionClient = Plugin.FullName,
                    SubmissionClientVersion = Plugin.Version,
                    ReleaseMbid = item.ProviderIds.GetValueOrDefault("MusicBrainzAlbum"),
                    ArtistMbids = item.ProviderIds.GetValueOrDefault("MusicBrainzArtist")?.Split(';'),
                    ReleaseGroupMbid = item.ProviderIds.GetValueOrDefault("MusicBrainzReleaseGroup"),
                    RecordingMbid = itemMetadata?.RecordingMbid,
                    TrackMbid = item.ProviderIds.GetValueOrDefault("MusicBrainzTrack"),
                    WorkMbids = null,
                    TrackNumber = item.IndexNumber,
                    Tags = item.Tags,
                    DurationMs = (item.RunTimeTicks / TimeSpan.TicksPerSecond) * 1000,
                }
            }
        };
    }

    /// <summary>
    /// Convenience method to get a MusicBrainz track ID for this item.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <returns>Track MBID. Null if not available.</returns>
    public static string? GetTrackMbid(this Audio item) => item.ProviderIds.GetValueOrDefault("MusicBrainzTrack");

    /// <summary>
    /// Create a <see cref="StoredListen"/> from this item.
    /// </summary>
    /// <param name="item">Item data source.</param>
    /// <param name="timestamp">UNIX timestamp of the listen.</param>
    /// <param name="metadata">Additional metadata.</param>
    /// <returns>An instance of <see cref="StoredListen"/> corresponding to the item.</returns>
    public static StoredListen AsStoredListen(this Audio item, long timestamp, AudioItemMetadata? metadata)
    {
        return new StoredListen
        {
            Id = item.Id,
            ListenedAt = timestamp,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Item runtime in seconds.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <returns>Runtime in seconds.</returns>
    public static long RuntimeSeconds(this Audio item) => TimeSpan.FromTicks(item.RunTimeTicks ?? 0).Seconds;

    /// <summary>
    /// Create a <see cref="TrackedItem"/> from this item.
    /// </summary>
    /// <param name="item">Item data source.</param>
    /// <param name="userId">Jellyfin user ID associated with this tracked item.</param>
    /// <returns>An instance of <see cref="TrackedItem"/>.</returns>
    public static TrackedItem AsTrackedItem(this Audio item, string userId)
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
