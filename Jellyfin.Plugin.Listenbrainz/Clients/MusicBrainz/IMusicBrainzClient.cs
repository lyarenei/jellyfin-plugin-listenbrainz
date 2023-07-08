using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models.Musicbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Clients.MusicBrainz
{
    /// <summary>
    /// Musicbrainz service client interface.
    /// </summary>
    public interface IMusicBrainzClient
    {
        /// <summary>
        /// Get recording data by track MBID.
        /// </summary>
        /// <param name="trackId">ID of the track.</param>
        /// <returns>An instance of <see cref="Recording"/>. Null if error or not found.</returns>
        public Task<Recording?> GetRecordingData(string trackId);
    }
}
