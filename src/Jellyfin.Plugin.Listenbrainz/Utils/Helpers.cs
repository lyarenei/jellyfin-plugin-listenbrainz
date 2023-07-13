using System;
using System.IO;

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
        /// Get path to listen cache file.
        /// </summary>
        /// <returns>Path to the file.</returns>
        public static string GetListenCacheFilePath() => Path.Join(Plugin.GetDataPath(), "cache.json");
    }
}
