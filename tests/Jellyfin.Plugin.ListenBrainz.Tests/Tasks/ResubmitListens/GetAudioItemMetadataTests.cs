using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.ResubmitListens;

public sealed class GetAudioItemMetadataTests : TestBase
{
    [Fact]
    public async Task ReturnsMetadata_WhenProviderSucceeds()
    {
        var audio = new Audio();
        var expected = new AudioItemMetadata();

        _metadataProviderServiceMock
            .Setup(x => x.GetAudioItemMetadataAsync(audio, CancellationToken.None))
            .ReturnsAsync(expected);

        var actual = await _task.GetAudioItemMetadataAsync(audio, CancellationToken.None);

        Assert.Same(expected, actual);
        _metadataProviderServiceMock.Verify(
            x => x.GetAudioItemMetadataAsync(audio, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task ReturnsNull_WhenProviderThrows()
    {
        var audio = new Audio();

        _metadataProviderServiceMock
            .Setup(x => x.GetAudioItemMetadataAsync(audio, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Assert.Null(await _task.GetAudioItemMetadataAsync(audio, CancellationToken.None));
        _metadataProviderServiceMock.Verify(
            x => x.GetAudioItemMetadataAsync(audio, CancellationToken.None),
            Times.Once);
    }
}
