using Jellyfin.Plugin.ListenBrainz.Extensions;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests;

public class ExtensionsTests
{
    [Theory]
    [InlineData("foobar", "Foobar")]
    [InlineData("", "")]
    [InlineData("f", "F")]
    [InlineData("FOOBAR", "FOOBAR")]
    public void Extensions_Capitalize(string s, string expected)
    {
        Assert.Equal(expected, s.Capitalize());
    }
}
