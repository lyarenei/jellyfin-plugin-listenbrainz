using Jellyfin.Plugin.Listenbrainz.Exceptions;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Extensions;

/// <summary>
/// Jellyfin Audio item extensions.
/// </summary>
public static class AudioExtensions
{
    /// <summary>
    /// Assert audio item has necessary metadata for listen submission.
    /// </summary>
    /// <param name="item">Audio item.</param>
    public static void AssertHasRequiredMetadata(this Audio item)
    {
        if (string.IsNullOrWhiteSpace(item.Artists[0])) throw new ItemMetadataException("Artist name is empty");
        if (string.IsNullOrWhiteSpace(item.Name)) throw new ItemMetadataException("Item name is empty");
    }
}
