using System.Globalization;

namespace Jellyfin.Plugin.ListenBrainz.Common.Extensions;

/// <summary>
/// String extensions.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Capitalize string.
    /// </summary>
    /// <param name="s">String to capitalize.</param>
    /// <returns>Capitalized string.</returns>
    public static string Capitalize(this string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpper(s[0], CultureInfo.InvariantCulture) + s[1..];
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
}
