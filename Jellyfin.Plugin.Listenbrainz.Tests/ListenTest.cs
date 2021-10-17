using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Listenbrainz.Models;
using Xunit;

namespace Jellyfin.Plugin.Listenbrainz.Tests
{
    public class ListenTest
    {
        private readonly JsonSerializerOptions options;

        public ListenTest()
        {
            options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }
        [Fact]
        public void Encode()
        {

            var listen = new Listen
            {
                ListenedAt = 1234,
                TrackMetadata = new TrackMetadata()
                {
                    ArtistName = "Foo Bar",
                    TrackName = "Foo - Bar",
                    ReleaseName = "Foobar",
                    AdditionalInfo = new AdditionalInfo()
                    {
                        ReleaseMbId = "release-foo",
                        ArtistMbIds = new List<string>() { "artist-foo" },
                        RecordingMbId = "recording-foo"
                    }
                }
            };

            var listenJson = JsonSerializer.Serialize(listen, options);
            Assert.NotNull(listenJson);

            var expectedJSON = @"{""listened_at"":1234,""track_metadata"":{""artist_name"":""Foo Bar"",""release_name"":""Foobar"",""track_name"":""Foo - Bar"",""additional_info"":{""artist_mbids"":[""artist-foo""],""recording_mbid"":""recording-foo"",""release_mbid"":""release-foo""}}}";
            Assert.Equal(expectedJSON, listenJson);
        }
    }
}
