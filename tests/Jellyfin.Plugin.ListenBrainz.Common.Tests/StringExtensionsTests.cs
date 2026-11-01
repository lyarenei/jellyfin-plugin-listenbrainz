using Jellyfin.Plugin.ListenBrainz.Common.Extensions;

namespace Jellyfin.Plugin.ListenBrainz.Common.Tests;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("foobar", "Foobar")]
    [InlineData("", "")]
    [InlineData("f", "F")]
    [InlineData("FOOBAR", "FOOBAR")]
    public void StringExtensions_Capitalize(string s, string expected)
    {
        Assert.Equal(expected, s.Capitalize());
    }

    [Theory]
    [InlineData("kebabCase", "kebab-case")]
    [InlineData("Kebabcase", "kebabcase")]
    [InlineData("KebAbcAse", "keb-abc-ase")]
    [InlineData("KebabcasE", "kebabcas-e")]
    public void StringExtensions_ConvertToKebabCase(string input, string expected)
    {
        Assert.Equal(expected, input.ToKebabCase());
    }

    [Theory]
    [InlineData("plain text", "plain text")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("\u0001", "")]
    [InlineData("\u001F", "")]
    [InlineData("abc\u001Fdef", "abcdef")]
    [InlineData("a\tb\nc\rd", "a\tb\nc\rd")]
    [InlineData("<&>\"'", "<&>\"'")]
    public void StringExtensions_SanitizeForXml(string? input, string expected)
    {
        Assert.Equal(expected, input.SanitizeForXml());
    }

    [Fact]
    public void StringExtensions_SanitizeForXml_KeepsCharactersOutsideTheBasicMultilingualPlane()
    {
        // Emoji are two-char characters (outside BMP), must be handled as single char
        // The char sub-halves are not valid on their own.
        const string Emoji = "\U0001F3B5";

        Assert.Equal(Emoji, Emoji.SanitizeForXml());
    }

    [Fact]
    public void StringExtensions_SanitizeForXml_ReplacesAnUnpairedSurrogate()
    {
        Assert.Equal("\uFFFD", "\uD800".SanitizeForXml());
    }
}
