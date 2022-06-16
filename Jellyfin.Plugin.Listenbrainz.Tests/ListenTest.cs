using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Listenbrainz.Json;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Tests;

public class ListenTest
{
    private readonly JsonSerializerOptions options;

    public ListenTest()
    {
        options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = SnakeCaseNamingPolicy.Instance
        };
    }

    public Listen _exampleListen = new Listen
    {
        ListenedAt = 1234,
        Data = new TrackMetadata
        {
            ArtistName = "Foo Bar",
            TrackName = "Foo - Bar",
            ReleaseName = "Foobar",
            Info = new AdditionalInfo
            {
                ReleaseMbId = "release-foo",
                ArtistMbIds = new Collection<string> { "artist-foo" },
                RecordingMbId = "recording-foo"
            }
        }
    };

    [Fact]
    public void Listen_Encode()
    {
        var listenJson = JsonSerializer.Serialize(_exampleListen, options);
        Assert.NotNull(listenJson);

        var expectedJSON = @"{""listened_at"":1234,""track_metadata"":{""artist_name"":""Foo Bar"",""release_name"":""Foobar"",""track_name"":""Foo - Bar"",""additional_info"":{""release_mbid"":""release-foo"",""artist_mbids"":[""artist-foo""],""recording_mbid"":""recording-foo""}}}";
        Assert.Equal(expectedJSON, listenJson);
    }

    [Fact]
    public void Listen_Encode_Decode()
    {
        var listenJson = JsonSerializer.Serialize(_exampleListen, options);
        var deserializedListen = JsonSerializer.Deserialize<Listen>(listenJson, options);
        Assert.NotNull(deserializedListen);
    }
}
