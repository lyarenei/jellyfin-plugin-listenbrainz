using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Extensions;

/// <summary>
/// Extensions for <see cref="Audio"/> type.
/// </summary>
public static class AudioExtensions
{
    /// <summary>
    /// Assert this item has required metadata for ListenBrainz submission.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <exception cref="ArgumentException">Item does not have required data.</exception>
    public static void AssertHasMetadata(this Audio item)
    {
        var artistNames = item.Artists.TakeWhile(name => !string.IsNullOrEmpty(name));
        if (!artistNames.Any())
        {
            throw new ArgumentException("Item has no artists");
        }

        if (string.IsNullOrWhiteSpace(item.Name))
        {
            throw new ArgumentException("Item name is empty");
        }
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
                    ArtistMbids = item.ProviderIds.GetValueOrDefault("MusicBrainzArtist")?.Split(';', '/', ',', (char)0x1F).Select(s => s.Trim()).ToArray(),
                    ReleaseGroupMbid = item.ProviderIds.GetValueOrDefault("MusicBrainzReleaseGroup"),
                    RecordingMbid = item.GetRecordingMbid() ?? itemMetadata?.RecordingMbid,
                    TrackMbid = item.GetTrackMbid(),
                    WorkMbids = null,
                    TrackNumber = item.IndexNumber,
                    Isrc = itemMetadata?.Isrcs.FirstOrDefault(),
                    Tags = item.Tags,
                    DurationMs = (item.RunTimeTicks / TimeSpan.TicksPerSecond) * 1000
                }
            }
        };
    }
}
