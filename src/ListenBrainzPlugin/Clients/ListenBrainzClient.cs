using ListenBrainzPlugin.Configuration;
using ListenBrainzPlugin.Dtos;
using ListenBrainzPlugin.Extensions;
using ListenBrainzPlugin.Interfaces;
using ListenBrainzPlugin.ListenBrainzApi.Interfaces;
using ListenBrainzPlugin.ListenBrainzApi.Models;
using ListenBrainzPlugin.ListenBrainzApi.Models.Requests;
using ListenBrainzPlugin.ListenBrainzApi.Resources;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace ListenBrainzPlugin.Clients;

/// <summary>
/// ListenBrainz client for plugin.
/// </summary>
public class ListenBrainzClient : IListenBrainzClient
{
    private readonly ILogger _logger;
    private readonly IListenBrainzApiClient _apiClient;
    private readonly ILibraryManager? _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzClient"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="apiClient">ListenBrainz API client instance.</param>
    public ListenBrainzClient(ILogger logger, IListenBrainzApiClient apiClient)
    {
        _logger = logger;
        _apiClient = apiClient;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzClient"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="apiClient">ListenBrainz API client instance.</param>
    /// <param name="libraryManager">Library manager.</param>
    public ListenBrainzClient(ILogger logger, IListenBrainzApiClient apiClient, ILibraryManager libraryManager)
    {
        _logger = logger;
        _apiClient = apiClient;
        _libraryManager = libraryManager;
    }

    /// <inheritdoc />
    public void SendNowPlaying(ListenBrainzUserConfig config, Audio item, AudioItemMetadata? audioMetadata)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.ApiToken,
            ListenType = ListenType.PlayingNow,
            Payload = new[] { item.AsListen(itemMetadata: audioMetadata) }
        };

        _apiClient.SubmitListens(request, CancellationToken.None);
    }

    /// <inheritdoc />
    public void SendListen(ListenBrainzUserConfig config, Audio item, AudioItemMetadata? metadata, long listenedAt)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.ApiToken,
            ListenType = ListenType.Single,
            Payload = new[] { item.AsListen(listenedAt, metadata) }
        };

        _apiClient.SubmitListens(request, CancellationToken.None);
    }

    /// <inheritdoc />
    public void SendFeedback(ListenBrainzUserConfig config, bool isFavorite, string? recordingMbid = null, string? recordingMsid = null)
    {
        var request = new RecordingFeedbackRequest
        {
            ApiToken = config.ApiToken,
            RecordingMbid = recordingMbid,
            RecordingMsid = recordingMsid,
            Score = isFavorite ? FeedbackScore.Loved : FeedbackScore.Neutral
        };

        _apiClient.SubmitRecordingFeedback(request, CancellationToken.None);
    }

    /// <inheritdoc />
    public void SendListens(ListenBrainzUserConfig config, IEnumerable<StoredListen> storedListens)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.ApiToken,
            ListenType = ListenType.Single,
            Payload = ToListens(storedListens)
        };

        _apiClient.SubmitListens(request, CancellationToken.None);
    }

    /// <summary>
    /// Convert all <see cref="StoredListen"/>s to <see cref="Listen"/>s.
    /// </summary>
    /// <param name="storedListens">Stored listens to convert.</param>
    /// <returns>Converted listens.</returns>
    private IEnumerable<Listen> ToListens(IEnumerable<StoredListen> storedListens)
    {
        if (_libraryManager is null) throw new InvalidOperationException("Library manager is not available");

        var listensToConvert = storedListens.ToArray();
        return listensToConvert
            .Select(l => _libraryManager.GetItemById(l.Id))
            .Cast<Audio>()
            .Select(a => a.AsListen(
                listensToConvert.First(l => l.Id == a.Id).ListenedAt,
                listensToConvert.First(l => l.Id == a.Id).Metadata));
    }
}
