using System.Collections.Generic;
using Jellyfin.Plugin.ListenBrainz.Http;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Http.Tests;

public class UtilsTests
{
    [Fact]
    public void Utils_ToHttpGetQueryEmptyDictOK()
    {
        var data = new Dictionary<string, string>();
        Assert.Equal(string.Empty, Utils.ToHttpGetQuery(data));
    }

    [Fact]
    public void Utils_ToHttpGetQueryOK()
    {
        var data = new Dictionary<string, string>
        {
            { "foo", "bar" },
            { "abc", "efg" },
            { "number", "42" },
            { "isOk", "false" }
        };

        const string Expected = "foo=bar&abc=efg&number=42&isOk=false";
        Assert.Equal(Expected, Utils.ToHttpGetQuery(data));
    }

    [Fact]
    public void Utils_ToHttpGetQueryUrlEncode()
    {
        var data = new Dictionary<string, string>
        {
            { "space", "a b" },
            { "special", "&//" },
            { "number", "42" }
        };

        const string Expected = "space=a+b&special=%26%2F%2F&number=42";
        Assert.Equal(Expected, Utils.ToHttpGetQuery(data));
    }
}
