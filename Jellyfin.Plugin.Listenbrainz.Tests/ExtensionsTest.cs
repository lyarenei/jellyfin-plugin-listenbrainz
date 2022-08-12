using Jellyfin.Plugin.Listenbrainz.Extensions;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Tests;

public class ExtensionsTest
{
    [Theory]
    [InlineData("snakeCase", "snake_case")]
    [InlineData("Snakecase", "snakecase")]
    [InlineData("SnAkeCaSe", "sn_ake_ca_se")]
    [InlineData("SnakecasE", "snakecas_e")]
    public void Extensions_ConvertToSnakeCase(string input, string expected)
    {
        Assert.Equal(expected, input.ToSnakeCase());
    }
}
