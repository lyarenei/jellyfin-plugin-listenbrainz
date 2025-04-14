using Jellyfin.Plugin.ListenBrainz.Api.Interfaces;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Requests;
using Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;
using Jellyfin.Plugin.ListenBrainz.Api.Resources;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Jellyfin.Plugin.ListenBrainz.Exceptions;
using Jellyfin.Plugin.ListenBrainz.Extensions;
using Jellyfin.Plugin.ListenBrainz.Interfaces;
using MediaBrowser.Controller.Entities.Audio;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Clients;

/// <summary>
/// ListenBrainz client for plugin.
/// </summary>
public class ListenBrainzClient : IListenBrainzClient
{
    private readonly ILogger _logger;
    private readonly IListenBrainzApiClient _apiClient;
    private readonly IPluginConfigService _pluginConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListenBrainzClient"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="apiClient">ListenBrainz API client instance.</param>
    /// <param name="pluginConfig">Plugin configuration service.</param>
    public ListenBrainzClient(ILogger logger, IListenBrainzApiClient apiClient, IPluginConfigService pluginConfig)
    {
        _logger = logger;
        _apiClient = apiClient;
        _pluginConfig = pluginConfig;
    }

    /// <inheritdoc />
    public void SendNowPlaying(UserConfig config, Audio item, AudioItemMetadata? audioMetadata)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.PlayingNow,
            Payload = new[] { item.AsListen(itemMetadata: audioMetadata) },
            BaseUrl = _pluginConfig.ListenBrainzApiUrl
        };

        var task = _apiClient.SubmitListens(request, CancellationToken.None);
        task.Wait();
        if (task.Exception is not null)
        {
            throw task.Exception;
        }

        if (task.Result.IsNotOk)
        {
            throw new PluginException("Sending now playing failed");
        }
    }

    /// <inheritdoc />
    public void SendListen(UserConfig config, Audio item, AudioItemMetadata? metadata, long listenedAt)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Single,
            Payload = new[] { item.AsListen(listenedAt, metadata) },
            BaseUrl = _pluginConfig.ListenBrainzApiUrl
        };

        var task = _apiClient.SubmitListens(request, CancellationToken.None);
        task.Wait();
        if (task.Exception is not null)
        {
            throw task.Exception;
        }

        if (task.Result.IsNotOk)
        {
            throw new PluginException("Sending listen failed");
        }
    }

    /// <inheritdoc />
    public void SendFeedback(
        UserConfig config,
        bool isFavorite,
        string? recordingMbid = null,
        string? recordingMsid = null)
    {
        var request = new RecordingFeedbackRequest
        {
            ApiToken = config.PlaintextApiToken,
            RecordingMbid = recordingMbid,
            RecordingMsid = recordingMsid,
            Score = isFavorite ? FeedbackScore.Loved : FeedbackScore.Neutral,
            BaseUrl = _pluginConfig.ListenBrainzApiUrl
        };

        var task = _apiClient.SubmitRecordingFeedback(request, CancellationToken.None);
        task.Wait();
        if (task.Exception is not null)
        {
            throw task.Exception;
        }

        if (task.Result.IsNotOk)
        {
            throw new PluginException("Sending feedback failed");
        }
    }

    /// <inheritdoc />
    /// <exception cref="PluginException">Sending listens failed.</exception>
    public async Task SendListensAsync(
        UserConfig config,
        IEnumerable<Listen> listens,
        CancellationToken cancellationToken)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Import,
            Payload = listens,
            BaseUrl = _pluginConfig.ListenBrainzApiUrl
        };

        SubmitListensResponse resp;
        try
        {
            resp = await _apiClient.SubmitListens(request, cancellationToken);
        }
        catch (Exception e)
        {
            throw new PluginException("SendListensAsync failed", e);
        }

        if (resp.IsNotOk)
        {
            throw new PluginException("Sending listens failed");
        }
    }

    /// <inheritdoc />
    public async Task<ValidatedToken> ValidateToken(string apiToken)
    {
        var request = new ValidateTokenRequest(apiToken) { BaseUrl = _pluginConfig.ListenBrainzApiUrl };
        var response = await _apiClient.ValidateToken(request, CancellationToken.None);
        return new ValidatedToken
        {
            IsValid = response.Valid,
            Reason = response.Message,
            UserName = response.UserName
        };
    }

    /// <inheritdoc />
    public string GetRecordingMsidByListenTs(UserConfig config, long ts)
    {
        var userName = config.UserName;
        if (string.IsNullOrEmpty(userName))
        {
            // Earlier 3.x plugin configurations did not store the username
            _logger.LogDebug("ListenBrainz username is not available, getting it via token validation");
            userName = GetListenBrainzUsername(config.PlaintextApiToken);
        }

        var request = new GetUserListensRequest(userName);
        var task = _apiClient.GetUserListens(request, CancellationToken.None);
        task.Wait();
        if (task.Exception is not null)
        {
            throw task.Exception;
        }

        var recordingMsid = task.Result.Payload.Listens.FirstOrDefault(l => l.ListenedAt == ts)?.RecordingMsid;
        return recordingMsid ?? throw new PluginException("No listen matching the timestamp found");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetLovedTracksAsync(UserConfig config, CancellationToken cancellationToken)
    {
        var recordingMbids = new List<string>();
        int offset = 0;
        GetUserFeedbackResponse response;
        do
        {
            var request = new GetUserFeedbackRequest(
                config.UserName,
                FeedbackScore.Loved,
                Limits.MaxItemsPerGet,
                offset);
            try
            {
                response = await _apiClient.GetUserFeedback(request, cancellationToken);
            }
            catch (Exception e)
            {
                throw new PluginException("GetUserFeedback failed", e);
            }

            if (response.IsNotOk)
            {
                throw new PluginException("Getting user loved tracks failed");
            }

            recordingMbids.AddRange(response.Feedback
                .Where(f => f.RecordingMbid is not null)
                .Select(f => f.RecordingMbid!));

            offset += response.Count;
        }
        while (offset < response.TotalCount);

        return recordingMbids;
    }

    /// <summary>
    /// Fetch ListenBrainz username using the API token.
    /// </summary>
    /// <param name="userApiToken">ListenBrainz API token.</param>
    /// <returns>ListenBrainz username associated with the API token.</returns>
    /// <exception cref="PluginException">Username could not be obtained.</exception>
    private string GetListenBrainzUsername(string userApiToken)
    {
        var tokenRequest = new ValidateTokenRequest(userApiToken) { BaseUrl = _pluginConfig.ListenBrainzApiUrl };
        var task = _apiClient.ValidateToken(tokenRequest, CancellationToken.None);
        task.Wait();
        if (task.Exception is not null)
        {
            throw task.Exception;
        }

        return task.Result.UserName ?? throw new PluginException("No username received");
    }
}
