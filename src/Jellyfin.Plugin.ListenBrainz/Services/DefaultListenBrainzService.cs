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

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// ListenBrainz client for plugin.
/// </summary>
public class DefaultListenBrainzService : IListenBrainzService
{
    private readonly ILogger _logger;
    private readonly IListenBrainzApiClient _apiClient;
    private readonly IPluginConfigService _pluginConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultListenBrainzService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="apiClient">ListenBrainz API client.</param>
    /// <param name="pluginConfig">Plugin configuration.</param>
    public DefaultListenBrainzService(ILogger logger, IListenBrainzApiClient apiClient, IPluginConfigService pluginConfig)
    {
        _logger = logger;
        _apiClient = apiClient;
        _pluginConfig = pluginConfig;
    }

    /// <inheritdoc />
    public async Task<bool> SendNowPlayingAsync(
        UserConfig config,
        Audio item,
        AudioItemMetadata? audioMetadata,
        CancellationToken cancellationToken)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.PlayingNow,
            Payload = [item.AsListen(itemMetadata: audioMetadata)],
            BaseUrl = _pluginConfig.ListenBrainzApiUrl,
        };

        try
        {
            var response = await _apiClient.SubmitListens(request, cancellationToken);
            return response.IsOk;
        }
        catch (Exception e)
        {
            _logger.LogDebug("Exception when sending now playing: {Message}", e.Message);
            throw new ServiceException("Sending now playing failed", e);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendListenAsync(
        UserConfig config,
        Audio item,
        AudioItemMetadata? metadata,
        long listenedAt,
        CancellationToken cancellationToken)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Single,
            Payload = [item.AsListen(listenedAt, metadata)],
            BaseUrl = _pluginConfig.ListenBrainzApiUrl,
        };

        try
        {
            var response = await _apiClient.SubmitListens(request, cancellationToken);
            return response.IsOk;
        }
        catch (Exception e)
        {
            _logger.LogDebug("Exception when sending listen: {Message}", e.Message);
            throw new ServiceException("Sending listen failed", e);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendListenAsync(UserConfig config, Listen listen, CancellationToken cancellationToken)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Single,
            Payload = [listen],
            BaseUrl = _pluginConfig.ListenBrainzApiUrl,
        };

        try
        {
            var response = await _apiClient.SubmitListens(request, cancellationToken);
            return response.IsOk;
        }
        catch (Exception e)
        {
            _logger.LogDebug("Exception when sending listen: {Message}", e.Message);
            throw new ServiceException("Sending listen failed", e);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendFeedbackAsync(
        UserConfig config,
        bool isFavorite,
        string? recordingMbid = null,
        string? recordingMsid = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(recordingMbid) && string.IsNullOrEmpty(recordingMsid))
        {
            throw new ArgumentException("No recording MBID or MSID provided");
        }

        var request = new RecordingFeedbackRequest
        {
            ApiToken = config.PlaintextApiToken,
            RecordingMbid = recordingMbid,
            RecordingMsid = recordingMsid,
            Score = isFavorite ? FeedbackScore.Loved : FeedbackScore.Neutral,
            BaseUrl = _pluginConfig.ListenBrainzApiUrl,
        };

        try
        {
            var response = await _apiClient.SubmitRecordingFeedback(request, cancellationToken);
            return response.IsOk;
        }
        catch (Exception e)
        {
            _logger.LogDebug("Exception when sending recording feedback: {Message}", e.Message);
            throw new ServiceException("Sending recording feedback failed", e);
        }
    }

    /// <inheritdoc />
    public async Task<bool> SendListensAsync(
        UserConfig config,
        IEnumerable<Listen> listens,
        CancellationToken cancellationToken)
    {
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Import,
            Payload = listens,
            BaseUrl = _pluginConfig.ListenBrainzApiUrl,
        };

        try
        {
            var response = await _apiClient.SubmitListens(request, cancellationToken);
            return response.IsOk;
        }
        catch (Exception e)
        {
            _logger.LogDebug("Exception when sending listens: {Message}", e.Message);
            throw new ServiceException("Sending listens failed", e);
        }
    }

    /// <inheritdoc />
    public async Task<ValidatedToken> ValidateTokenAsync(string apiToken, CancellationToken cancellationToken)
    {
        var request = new ValidateTokenRequest(apiToken) { BaseUrl = _pluginConfig.ListenBrainzApiUrl };
        try
        {
            var response = await _apiClient.ValidateToken(request, cancellationToken);
            if (response.IsNotOk)
            {
                return new ValidatedToken();
            }

            return new ValidatedToken
            {
                IsValid = response.Valid,
                Reason = response.Message,
                UserName = response.UserName,
            };
        }
        catch (Exception e)
        {
            _logger.LogDebug("Exception when validating token: {Message}", e.Message);
            throw new ServiceException("Token validation failed", e);
        }
    }

    /// <inheritdoc />
    public async Task<string> GetRecordingMsidByListenTsAsync(
        UserConfig config,
        long ts,
        CancellationToken cancellationToken)
    {
        var userName = config.UserName;
        if (string.IsNullOrEmpty(userName))
        {
            // Earlier 3.x plugin configurations did not store the username
            _logger.LogDebug("ListenBrainz username is not available, getting it via token validation");
            userName = await GetListenBrainzUsernameAsync(config.PlaintextApiToken, cancellationToken);
        }

        var request = new GetUserListensRequest(userName)
        {
            ApiToken = config.ApiToken,
            BaseUrl = _pluginConfig.ListenBrainzApiUrl,
        };

        try
        {
            var response = await _apiClient.GetUserListens(request, cancellationToken);
            var recordingMsid = response.Payload.Listens.FirstOrDefault(l => l.ListenedAt == ts)?.RecordingMsid;
            return recordingMsid ?? string.Empty;
        }
        catch (Exception e)
        {
            _logger.LogDebug("Exception when getting user listens: {Message}", e.Message);
            throw new ServiceException("Getting user listens failed", e);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetLovedTracksAsync(UserConfig config, CancellationToken cancellationToken)
    {
        var recordingMbids = new List<string>();
        int offset = 0;
        GetUserFeedbackResponse response;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new GetUserFeedbackRequest(
                config.UserName,
                FeedbackScore.Loved,
                Limits.MaxItemsPerGet,
                offset)
            {
                ApiToken = config.ApiToken,
                BaseUrl = _pluginConfig.ListenBrainzApiUrl,
            };

            try
            {
                response = await _apiClient.GetUserFeedback(request, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogDebug("Exception when getting user feedback: {Message}", e.Message);
                throw new ServiceException("Getting user loved tracks failed", e);
            }

            if (response.IsNotOk)
            {
                throw new ServiceException("Getting user loved tracks failed");
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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>ListenBrainz username associated with the API token.</returns>
    private async Task<string> GetListenBrainzUsernameAsync(string userApiToken, CancellationToken cancellationToken)
    {
        var tokenData = await ValidateTokenAsync(userApiToken, cancellationToken);
        if (!tokenData.IsValid)
        {
            throw new ServiceException("Token is not valid");
        }

        if (string.IsNullOrEmpty(tokenData.UserName))
        {
            throw new ServiceException("No username received");
        }

        return tokenData.UserName;
    }
}
