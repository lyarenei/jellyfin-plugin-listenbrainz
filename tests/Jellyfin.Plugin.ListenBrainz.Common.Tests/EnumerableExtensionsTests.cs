using Jellyfin.Plugin.ListenBrainz.Common.Extensions;

namespace Jellyfin.Plugin.ListenBrainz.Common.Tests;

public class EnumerableExtensionsTests
{
    private readonly List<string?> _listWithNulls = ["foo", null, "bar"];

    [Fact]
    public void EnumerableExtensionsTests_WhereNotNull()
    {
        var expected = new[] { "foo", "bar" };
        Assert.Equal(expected, _listWithNulls.WhereNotNull());
    }
}
