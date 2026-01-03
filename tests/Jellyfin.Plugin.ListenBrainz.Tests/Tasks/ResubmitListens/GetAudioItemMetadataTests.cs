using System;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using Moq;
using Xunit;

namespace Jellyfin.Plugin.ListenBrainz.Tests.Tasks.ResubmitListens;

public sealed class GetAudioItemMetadataTests : TestBase
{
    [Fact]
    public void ReturnsNull_WhenItemIsNotAudio()
    {
        var listen = new StoredListen { Id = Guid.Empty };

        _libraryManagerMock
            .Setup(x => x.GetItemById(listen.Id))
            .Returns(new Movie());

        var result = _task.GetAudioItemMetadata(listen, CancellationToken.None);

        Assert.Null(result);
        _metadataProviderServiceMock.Verify(
            x => x.GetAudioItemMetadataAsync(It.IsAny<Audio>(), CancellationToken.None),
            Times.Never);
    }

    [Fact]
    public void ReturnsMetadata_WhenProviderSucceeds()
    {
        var listen = new StoredListen { Id = Guid.Empty };
        var audio = new Mock<Audio>().Object;
        var expected = new AudioItemMetadata();

        _libraryManagerMock
            .Setup(x => x.GetItemById(listen.Id))
            .Returns(audio);

        _metadataProviderServiceMock
            .Setup(x => x.GetAudioItemMetadataAsync(audio, CancellationToken.None))
            .ReturnsAsync(expected);

        var actual = _task.GetAudioItemMetadata(listen, CancellationToken.None);

        Assert.Same(expected, actual);
        _metadataProviderServiceMock.Verify(
            x => x.GetAudioItemMetadataAsync(audio, CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public void ThrowsServiceException_WhenProviderFails()
    {
        var listen = new StoredListen { Id = Guid.Empty };
        var audio = new Audio();

        _libraryManagerMock
            .Setup(x => x.GetItemById(listen.Id))
            .Returns(audio);

        _metadataProviderServiceMock
            .Setup(x => x.GetAudioItemMetadataAsync(audio, CancellationToken.None))
            .Returns(Task.FromResult<AudioItemMetadata?>(null));

        Assert.Null(_task.GetAudioItemMetadata(listen, CancellationToken.None));
        _libraryManagerMock.Verify(x => x.GetItemById(listen.Id), Times.Once);
        _metadataProviderServiceMock.Verify(
            x => x.GetAudioItemMetadataAsync(audio, CancellationToken.None),
            Times.Once);
    }
}
