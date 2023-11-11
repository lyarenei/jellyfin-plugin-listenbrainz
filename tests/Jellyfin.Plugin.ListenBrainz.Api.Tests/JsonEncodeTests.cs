using System.Collections.ObjectModel;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Newtonsoft.Json;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Api.Tests;

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
        var listenJson = JsonConvert.SerializeObject(_exampleListen, BaseClient.SerializerSettings);
        Assert.NotNull(listenJson);

        var expectedJSON = @"{""listened_at"":1234,""track_metadata"":{""artist_name"":""Foo Bar"",""track_name"":""Foo - Bar"",""release_name"":""Foobar"",""additional_info"":{""artist_mbids"":[""artist-foo""],""release_mbid"":""release-foo"",""recording_mbid"":""recording-foo""}}}";
        Assert.Equal(expectedJSON, listenJson);
    }

    [Fact]
    public void Listen_EncodeAndDecode()
    {
        var listenJson = JsonConvert.SerializeObject(_exampleListen, BaseClient.SerializerSettings);
        var deserializedListen = JsonConvert.DeserializeObject<Listen>(listenJson, BaseClient.SerializerSettings);
        Assert.NotNull(deserializedListen);
    }
}

public class RecordingFeedbackTests
{
    [Theory]
    [InlineData(FeedbackScore.Hated)]
    [InlineData(FeedbackScore.Loved)]
    [InlineData(FeedbackScore.Neutral)]
    public void FeedbackValues_Encode(FeedbackScore score)
    {
        var request = new RecordingFeedbackRequest { Score = score };
        var actualJson = JsonConvert.SerializeObject(request, BaseClient.SerializerSettings);
        Assert.NotNull(actualJson);

        var expectedJson = @"{""score"":" + (int)score + "}";
        Assert.Equal(expectedJson, actualJson);
    }
}
