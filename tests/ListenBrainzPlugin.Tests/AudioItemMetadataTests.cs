using ListenBrainzPlugin.Dtos;
using Xunit;

namespace ListenBrainzPlugin.Tests;

public class AudioItemMetadataTests
{
    private readonly ArtistCredit _artistCredit1 = new("Artist 1", " with ");
    private readonly ArtistCredit _artistCredit2 = new("Artist 2");

    [Fact]
    public void AudioItemMetadata_GetCreditString()
    {
        var creditList = new[] { _artistCredit1, _artistCredit2 };
        var metadata = new AudioItemMetadata { ArtistCredits = creditList };
        Assert.Equal("Artist 1 with Artist 2", metadata.FullCreditString);
    }
}
