using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using MediaBrowser.Controller.Entities.Audio;
using static Jellyfin.Plugin.Listenbrainz.Resources.Listenbrainz;

namespace Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz.Requests;

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
    /// Gets listen type.
    /// </summary>
    public string ListenType { get; }

    /// <summary>
    /// Gets a collection of payloads for the request.
    /// </summary>
    [JsonPropertyName("payload")]
    public Collection<Listen> Data { get; }

    /// <inheritdoc />
    public override string GetEndpoint() => Endpoints.SubmitListen;

    /// <summary>
    /// Determines if there is a Recording MBID available.
    /// </summary>
    /// <returns>Recording MBID is available.</returns>
    public bool HasRecordingMbId()
    {
        var recordingId = Data[0].Data.Info?.RecordingMbId;
        return recordingId != null;
    }

    /// <summary>
    /// Sets a recording MBID.
    /// </summary>
    /// <param name="recordingMbId">Recording MBID.</param>
    public void SetRecordingMbId(string? recordingMbId)
    {
        if (recordingMbId == null)
        {
            return;
        }

        if (Data[0].Data.Info == null)
        {
            Data[0].Data.Info = new AdditionalInfo(recordingMbId);
            return;
        }

        Data[0].Data.Info!.RecordingMbId = recordingMbId;
    }

    /// <summary>
    /// Gets a track MBID.
    /// </summary>
    /// <returns>Track MBID.</returns>
    public string? GetTrackMbId()
    {
        return Data[0].Data.Info?.TrackMbId;
    }
}
