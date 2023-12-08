using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Clients;

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
    public void SendNowPlaying(UserConfig config, Audio item, AudioItemMetadata? audioMetadata)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.PlayingNow,
            Payload = new[] { item.AsListen(itemMetadata: audioMetadata) },
            BaseUrl = pluginConfig.ListenBrainzApiUrl
        };

        _apiClient.SubmitListens(request, CancellationToken.None);
    }

    /// <inheritdoc />
    public void SendListen(UserConfig config, Audio item, AudioItemMetadata? metadata, long listenedAt)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Single,
            Payload = new[] { item.AsListen(listenedAt, metadata) },
            BaseUrl = pluginConfig.ListenBrainzApiUrl
        };

        _apiClient.SubmitListens(request, CancellationToken.None);
    }

    /// <inheritdoc />
    public void SendFeedback(UserConfig config, bool isFavorite, string? recordingMbid = null, string? recordingMsid = null)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var request = new RecordingFeedbackRequest
        {
            ApiToken = config.PlaintextApiToken,
            RecordingMbid = recordingMbid,
            RecordingMsid = recordingMsid,
            Score = isFavorite ? FeedbackScore.Loved : FeedbackScore.Neutral,
            BaseUrl = pluginConfig.ListenBrainzApiUrl
        };

        _apiClient.SubmitRecordingFeedback(request, CancellationToken.None);
    }

    /// <inheritdoc />
    public void SendListens(UserConfig config, IEnumerable<StoredListen> storedListens)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Import,
            Payload = ToListens(storedListens),
            BaseUrl = pluginConfig.ListenBrainzApiUrl
        };

        _apiClient.SubmitListens(request, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<ValidatedToken> ValidateToken(string apiToken)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var request = new ValidateTokenRequest(apiToken) { BaseUrl = pluginConfig.ListenBrainzApiUrl };
        var response = await _apiClient.ValidateToken(request, CancellationToken.None);
        if (response is null) throw new PluginException("Did not receive response");
        return new ValidatedToken
        {
            IsValid = response.Valid,
            Reason = response.Message
        };
    }

    /// <inheritdoc />
    public async Task<string?> GetRecordingMsidByListenTs(UserConfig config, long ts)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var tokenRequest = new ValidateTokenRequest(config.PlaintextApiToken) { BaseUrl = pluginConfig.ListenBrainzApiUrl };
        var tokenResponse = await _apiClient.ValidateToken(tokenRequest, CancellationToken.None);
        if (tokenResponse is null)
        {
            _logger.LogDebug("Did not receive response for token validate request, giving up");
            return null;
        }

        if (tokenResponse.UserName is null)
        {
            _logger.LogDebug("ValidateToken response does not contain username, cannot continue");
            return null;
        }

        var request = new GetUserListensRequest(tokenResponse.UserName);
        var response = await _apiClient.GetUserListens(request, CancellationToken.None);
        if (response is null)
        {
            throw new PluginException("Did not receive response for user listens request");
        }

        return response.Payload.Listens.FirstOrDefault(l => l.ListenedAt == ts)?.RecordingMsid;
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
