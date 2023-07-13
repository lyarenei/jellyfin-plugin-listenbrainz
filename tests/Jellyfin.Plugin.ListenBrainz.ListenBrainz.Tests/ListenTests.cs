using System.Collections.ObjectModel;
using System.Text.Json;
using Jellyfin.Plugin.ListenBrainz.ListenBrainz.Models.Dtos;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.ListenBrainz.Tests;

public class ListenTests
{
    private readonly Listen _exampleListen = new("Foo Bar", "Foo - Bar")
    {
        ListenedAt = 1234,
        TrackMetadata = new TrackMetadata("Foo Bar", "Foo - Bar")
        {
            ReleaseName = "Foobar",
            AdditionalInfo = new AdditionalInfo
            {
                ReleaseMbid = "release-foo",
                ArtistMbids = new Collection<string> { "artist-foo" },
                RecordingMbid = "recording-foo"
            }
        }
    };

    [Fact]
    public void Listen_Encode()
    {
        var listenJson = JsonSerializer.Serialize(_exampleListen, BaseClient.SerializerOptions);
        Assert.NotNull(listenJson);

        var expectedJSON = @"{""listened_at"":1234,""track_metadata"":{""artist_name"":""Foo Bar"",""track_name"":""Foo - Bar"",""release_name"":""Foobar"",""additional_info"":{""release_mbid"":""release-foo"",""artist_mbids"":[""artist-foo""],""recording_mbid"":""recording-foo""}}}";
        Assert.Equal(expectedJSON, listenJson);
    }

    [Fact]
    public void Listen_EncodeAndDecode()
    {
        var listenJson = JsonSerializer.Serialize(_exampleListen, BaseClient.SerializerOptions);
        var deserializedListen = JsonSerializer.Deserialize<Listen>(listenJson, BaseClient.SerializerOptions);
        Assert.NotNull(deserializedListen);
    }
}
