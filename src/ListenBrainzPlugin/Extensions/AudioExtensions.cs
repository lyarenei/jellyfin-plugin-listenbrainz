using ListenBrainzPlugin.ListenBrainzApi.Models;
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
    /// <returns>Listen instance with data from the item.</returns>
    public static Listen AsListen(this Audio item, long? timestamp = null)
    {
        return new Listen
        {
            ListenedAt = timestamp,
            TrackMetadata = new TrackMetadata
            {
                ArtistName = string.Join(", ", item.Artists.TakeWhile(name => !string.IsNullOrEmpty(name))),
                ReleaseName = item.Album,
                TrackName = item.Name,
                AdditionalInfo = new AdditionalInfo
                {
                    MediaPlayer = "Jellyfin",
                    MediaPlayerVersion = null,
                    SubmissionClient = "ListenBrainz plugin for Jellyfin",
                    SubmissionClientVersion = Plugin.Version,
                    ReleaseMbid = item.ProviderIds.GetValueOrDefault("MusicBrainzAlbum"),
                    ArtistMbids = item.ProviderIds.GetValueOrDefault("MusicBrainzArtist")?.Split(';'),
                    ReleaseGroupMbid = item.ProviderIds.GetValueOrDefault("MusicBrainzReleaseGroup"),
                    RecordingMbid = null,
                    TrackMbid = item.ProviderIds.GetValueOrDefault("MusicBrainzTrack"),
                    WorkMbids = null,
                    TrackNumber = item.IndexNumber,
                    Tags = item.Tags,
                    DurationMs = (item.RunTimeTicks / TimeSpan.TicksPerSecond) * 1000,
                }
            }
        };
    }
}
