using System.Collections.ObjectModel;
using ListenBrainzPlugin.MusicBrainzApi.Models;
using Xunit;

namespace ListenBrainzPlugin.MusicBrainzApi.Tests;

public class RecordingTests
{
    private readonly ArtistCredit _artistCredit1 = new()
    {
        JoinPhrase = " with ",
        Name = "Artist 1"
    };

    private readonly ArtistCredit _artistCredit2 = new()
    {
        Name = "Artist 2"
    };

    private readonly Recording _example = new()
    {
        Mbid = "recordingId",
        Score = 99
    };

    [Fact]
    public void Recording_GetCreditString()
    {
        var creditList = new[] { _artistCredit1, _artistCredit2 };
        _example.ArtistCredits = new Collection<ArtistCredit>(creditList);
        Assert.Equal("Artist 1 with Artist 2", _example.FullCreditString);
    }
}