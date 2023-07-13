using System.Threading.Tasks;
using Jellyfin.Plugin.ListenBrainz.MusicBrainz.Models.Dtos;

namespace Jellyfin.Plugin.Listenbrainz.Interfaces;

/// <summary>
/// MusicBrainz client.
/// </summary>
public interface IMusicBrainzClient
{
    /// <summary>
    /// Get a <see cref="Recording"/> by specified track MBID.
    /// </summary>
    /// <param name="trackId">Track MBID.</param>
    /// <returns>Recording.</returns>
    public Task<Recording> GetRecordingByTrackId(string trackId);
}
