using System;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using MediaBrowser.Controller.Entities.Audio;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Extensions;

public class AudioExtensionsTests
{
    private const string ArtistMbid1 = "17f0182e-146a-4115-8c83-aa291b4e3acc";
    private const string ArtistMbid2 = "bb53bece-e3ed-4a9e-9348-265745dcb239";

    private static Audio GetAudio(string artistMbidsValue)
    {
        var audio = new Audio
        {
            Id = Guid.NewGuid(),
            Name = "song",
            Artists = ["artist"],
        };

        audio.ProviderIds["MusicBrainzArtist"] = artistMbidsValue;
        return audio;
    }

    [Theory]
    [InlineData(";")]
    [InlineData(",")]
    [InlineData("/")]
    public void AsListen_SplitsArtistMbids_OnDefaultDelimiters(string delimiter)
    {
        var audio = GetAudio($"{ArtistMbid1}{delimiter}{ArtistMbid2}");

        var listen = audio.AsListen();

        Assert.Equal(
            [ArtistMbid1, ArtistMbid2],
            listen.TrackMetadata.AdditionalInfo!.ArtistMbids);
    }

    [Fact]
    public void AsListen_SplitsArtistMbids_OnUnitSeparatorDelimiter()
    {
        var unitSeparator = char.ConvertFromUtf32(0x1F);
        var audio = GetAudio($"{ArtistMbid1}{unitSeparator}{ArtistMbid2}");

        var listen = audio.AsListen();

        Assert.Equal(
            [ArtistMbid1, ArtistMbid2],
            listen.TrackMetadata.AdditionalInfo!.ArtistMbids);
    }

    [Fact]
    public void AsListen_SplitsArtistMbids_OnUnitSeparatorDelimiter_WithCustomDelimiters()
    {
        // The unit separator cannot be stored in the XML config, so it always applies
        // on top of whatever delimiters are configured.
        var unitSeparator = char.ConvertFromUtf32(0x1F);
        var audio = GetAudio($"{ArtistMbid1}{unitSeparator}{ArtistMbid2}");

        var listen = audio.AsListen(mbidDelimiters: "|");

        Assert.Equal(
            [ArtistMbid1, ArtistMbid2],
            listen.TrackMetadata.AdditionalInfo!.ArtistMbids);
    }

    [Fact]
    public void AsListen_TrimsWhitespace_AfterSplitting()
    {
        var audio = GetAudio($"{ArtistMbid1} , {ArtistMbid2}");

        var listen = audio.AsListen();

        Assert.Equal(
            [ArtistMbid1, ArtistMbid2],
            listen.TrackMetadata.AdditionalInfo!.ArtistMbids);
    }

    [Fact]
    public void AsListen_UsesCustomDelimiters_WhenProvided()
    {
        var audio = GetAudio($"{ArtistMbid1}|{ArtistMbid2}");

        var listen = audio.AsListen(mbidDelimiters: "|");

        Assert.Equal(
            [ArtistMbid1, ArtistMbid2],
            listen.TrackMetadata.AdditionalInfo!.ArtistMbids);
    }

    [Fact]
    public void AsListen_DoesNotSplitOnDefaultDelimiters_WhenCustomDelimitersProvided()
    {
        var combinedValue = ArtistMbid1 + "," + ArtistMbid2;
        var audio = GetAudio(combinedValue);

        var listen = audio.AsListen(mbidDelimiters: "|");

        Assert.Equal([combinedValue], listen.TrackMetadata.AdditionalInfo!.ArtistMbids);
    }

    [Fact]
    public void AsListen_FallsBackToDefaultDelimiters_WhenNoneProvided()
    {
        var audio = GetAudio($"{ArtistMbid1};{ArtistMbid2}");

        var listen = audio.AsListen(mbidDelimiters: null);

        Assert.Equal(
            [ArtistMbid1, ArtistMbid2],
            listen.TrackMetadata.AdditionalInfo!.ArtistMbids);
    }

    [Fact]
    public void AsListen_DoesNotSplit_WhenEmptyStringProvided()
    {
        var combinedValue = $"{ArtistMbid1};{ArtistMbid2}";
        var audio = GetAudio(combinedValue);

        var listen = audio.AsListen(mbidDelimiters: string.Empty);

        Assert.Equal([combinedValue], listen.TrackMetadata.AdditionalInfo!.ArtistMbids);
    }

    [Fact]
    public void DefaultMbidDelimitersValue_MatchesConfigurationDefault()
    {
        var config = new PluginConfiguration();

        Assert.Equal(PluginConfiguration.DefaultMbidDelimiters, config.MbidDelimiters);
    }
}
