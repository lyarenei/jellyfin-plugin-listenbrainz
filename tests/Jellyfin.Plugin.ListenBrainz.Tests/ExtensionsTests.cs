using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Xunit;

// TODO: move to .Common.Tests when more tests are written

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
