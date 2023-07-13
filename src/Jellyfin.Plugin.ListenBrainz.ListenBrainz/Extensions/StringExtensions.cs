using System.Globalization;

namespace Jellyfin.Plugin.Listenbrainz.ListenBrainz.Extensions;

/// <summary>
/// Extensions for a string type.
/// </summary>
public static class StringExtensions
{
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
}
