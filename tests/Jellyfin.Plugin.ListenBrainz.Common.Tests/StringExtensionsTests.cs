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
}
