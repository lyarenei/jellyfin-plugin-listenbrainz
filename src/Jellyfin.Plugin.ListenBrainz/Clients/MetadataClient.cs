using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
using MetaBrainz.MusicBrainz;

namespace Jellyfin.Plugin.ListenBrainz.Clients;

/// <summary>
/// An implementation of <see cref="IMetadataClient"/>.
/// Wrapper for <see cref="Query"/> methods.
/// </summary>
public sealed class MetadataClient : IMetadataClient, IDisposable
{
    private readonly Query _query;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetadataClient"/> class.
    /// </summary>
    /// <param name="clientName">Name of the client application..</param>
    /// <param name="version">Version of the client application.</param>
    /// <param name="contactUrl">Contact URL for the maintainer of the application.</param>
    public MetadataClient(string clientName, string version, string contactUrl)
    {
        _query = new Query(clientName, version, contactUrl);
        _isDisposed = false;
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="MetadataClient"/> class.
    /// </summary>
    ~MetadataClient() => Dispose(false);

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Dispose managed and unmanaged (own) resources.
    /// </summary>
    /// <param name="isDisposing">Dispose managed resources.</param>
    private void Dispose(bool isDisposing)
    {
        if (_isDisposed)
        {
            return;
        }

        if (isDisposing)
        {
            _query.Dispose();
        }

        _isDisposed = true;
    }

    /// <inheritdoc />
    public AudioItemMetadata GetAudioItemMetadata(Audio item)
    {
        var trackMbid = item.GetTrackMbid();
        if (trackMbid is null)
        {
            throw new ArgumentException("Audio item does not have a track MBID");
        }

        var recordings = _query.FindRecordings($"tid:{trackMbid}", 3);
        if (recordings.TotalResults < 1)
        {
            throw new PluginException("No results matching the track MBID");
        }

        var recording = recordings.Results[0].Item;
        return new AudioItemMetadata(recording);
    }
}
