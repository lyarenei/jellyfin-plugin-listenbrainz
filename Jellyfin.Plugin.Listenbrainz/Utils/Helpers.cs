using System;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Utils
{
    /// <summary>
    /// Various helpers.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Convert datetime to UNIX timestamp.
        /// </summary>
        /// <param name="dateTime">Datetime to convert.</param>
        /// <returns>UNIX timestamp.</returns>
        public static long TimestampFromDatetime(DateTime dateTime) => new DateTimeOffset(dateTime).ToUnixTimeSeconds();

        /// <summary>
        /// Get current time in UNIX time.
        /// </summary>
        /// <returns>UNIX timestamp.</returns>
        public static long GetCurrentTimestamp() => TimestampFromDatetime(DateTime.UtcNow);

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
}
