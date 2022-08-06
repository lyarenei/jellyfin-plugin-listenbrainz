using System;
using System.Globalization;
using System.Linq;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Utils
{
    /// <summary>
    /// Various helpers.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Get current time in UNIX time.
        /// </summary>
        /// <returns>UNIX timestamp.</returns>
        public static long GetCurrentTimestamp() => new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

        /// <summary>
        /// Converts string to snake_case.
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <returns>Converted string.</returns>
        /// From: https://stackoverflow.com/a/58576400.
        public static string ToSnakeCase(this string str)
        {
            var newStr = string.Concat(str.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString()));
            return newStr.ToLower(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts string to kebab-case.
        /// </summary>
        /// <param name="str">String to convert.</param>
        /// <returns>Converted string.</returns>
        /// Inspired by: https://stackoverflow.com/a/58576400.
        public static string ToKebabCase(this string str)
        {
            var newStr = string.Concat(str.Select((c, i) => i > 0 && char.IsUpper(c) ? "-" + c : c.ToString()));
            return newStr.ToLower(CultureInfo.InvariantCulture);
        }

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
