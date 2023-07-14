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
}
