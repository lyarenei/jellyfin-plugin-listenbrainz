using System.Collections.Generic;
using Xunit;

namespace ListenBrainzPlugin.Http.Tests;

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

        const string expected = "foo=bar&abc=efg&number=42&isOk=false";
        Assert.Equal(expected, Utils.ToHttpGetQuery(data));
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

        const string expected = "space=a+b&special=%26%2f%2f&number=42";
        Assert.Equal(expected, Utils.ToHttpGetQuery(data));
    }
}
