namespace ListenBrainzPlugin.Extensions;

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
        if (s == string.Empty) return s;
        return char.ToUpper(s[0]) + s[1..];
    }
}
