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
public class MetadataClient : IMetadataClient
{
    private readonly Query _query;

    /// <summary>
    ///
    /// </summary>
    /// <param name="clientName"></param>
    /// <param name="version"></param>
    /// <param name="sourceUrl"></param>
    /// <exception cref="NotImplementedException"></exception>
    public MetadataClient(string clientName, string version, string sourceUrl)
    {
        _query = new Query(clientName, version, sourceUrl);
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

        var recording = recordings.Results.First().Item;
        return new AudioItemMetadata(recording);
    }
}
