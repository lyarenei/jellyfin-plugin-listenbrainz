using System.Globalization;

namespace Jellyfin.Plugin.Listenbrainz.MusicBrainz.Extensions;

/// <summary>
/// Extensions for a string type.
/// </summary>
public static class StringExtensions
{
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
}
