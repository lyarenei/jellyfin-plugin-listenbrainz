using System.Globalization;

namespace Jellyfin.Plugin.ListenBrainz.Extensions;

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
}
