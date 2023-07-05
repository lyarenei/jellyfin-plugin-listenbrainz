using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Extensions;

/// <summary>
/// Jellyfin Audio item extensions.
/// </summary>
public static class AudioExtensions
{
    /// <summary>
    /// Checks if audio item has necessary metadata for listen submission.
    /// </summary>
    /// <param name="item">Audio item.</param>
    /// <returns>Audio item can be used for listen submission.</returns>
    public static bool HasRequiredMetadata(this Audio item)
    {
        var hasArtistName = !string.IsNullOrWhiteSpace(item.Artists[0]);
        var hasName = !string.IsNullOrWhiteSpace(item.Name);
        return hasArtistName && hasName;
    }
}
