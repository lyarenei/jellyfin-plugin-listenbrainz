using System.Globalization;
using System.Text;
using System.Xml;

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

    /// <summary>
    /// Sanitize string for XML format.
    /// </summary>
    /// <param name="value">String to sanitize.</param>
    /// <returns>String without any character that XML cannot represent.</returns>
    /// <remarks>
    /// Not every character can appear in an XML document, and not every XML writer refuses to
    /// write one which cannot. Such writer can produce invalid document, that will not be readable.
    /// </remarks>
    public static string SanitizeForXml(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var rune in value.EnumerateRunes())
        {
            // All multi-char characters (emoji, etc...) are valid in XML
            // only check single-char ones (these all fall in the Unicode BMP)
            if (!rune.IsBmp || XmlConvert.IsXmlChar((char)rune.Value))
            {
                builder.Append(rune);
            }
        }

        return builder.ToString();
    }
}
