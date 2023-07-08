using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.Listenbrainz.Resources.ListenBrainz;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests
{
    /// <summary>
    /// Request model for submitting listens.
    /// </summary>
    public class SubmitListenRequest : BaseRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitListenRequest"/> class.
        /// </summary>
        /// <param name="listenType">Type of listen.</param>
        /// <param name="item">Audio item with data.</param>
        /// <param name="listenedAt">Listened at in UNIX timestamp.</param>
        public SubmitListenRequest(string listenType, Audio item, long? listenedAt = null)
        {
            ListenType = listenType;
            Data = new Collection<Listen> { new(item, listenedAt) };
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubmitListenRequest"/> class.
        /// </summary>
        /// <param name="listenType">Type of listen.</param>
        /// <param name="listen">Listen data.</param>
        public SubmitListenRequest(string listenType, Listen listen)
        {
            ListenType = listenType;
            Data = new Collection<Listen> { listen };
        }

        /// <summary>
        /// Gets listen type.
        /// </summary>
        public string ListenType { get; }

        /// <summary>
        /// Gets a payload for the request.
        /// </summary>
        [JsonPropertyName("payload")]
        public Collection<Listen> Data { get; }

        /// <summary>
        /// Gets a track MBID.
        /// </summary>
        /// <returns>Track MBID.</returns>
        [JsonIgnore]
        public string? TrackMBID => Data[0].Data.Info?.TrackMbId;

        /// <inheritdoc />
        public override string GetEndpoint() => Endpoints.SubmitListen;

        /// <summary>
        /// Sets a recording MBID.
        /// </summary>
        /// <param name="recordingMbId">Recording MBID.</param>
        public void SetRecordingMbId(string? recordingMbId)
        {
            if (recordingMbId == null) return;
            if (Data[0].Data.Info == null)
            {
                Data[0].Data.Info = new AdditionalInfo(recordingMbId);
                return;
            }

            Data[0].Data.Info!.RecordingMbId = recordingMbId;
        }

        /// <summary>
        /// Set artist name.
        /// </summary>
        /// <param name="artistName">Artist name.</param>
        public void SetArtist(string artistName) => Data[0].Data.ArtistName = artistName;
    }
}
