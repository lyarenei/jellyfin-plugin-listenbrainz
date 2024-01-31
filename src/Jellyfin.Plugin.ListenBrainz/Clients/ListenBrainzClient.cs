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
        var pluginConfig = Plugin.GetConfiguration();
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Single,
            Payload = new[] { item.AsListen(listenedAt, metadata) },
            BaseUrl = pluginConfig.ListenBrainzApiUrl
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

        var task = _apiClient.SubmitListens(request, CancellationToken.None);
        task.Wait();
        if (task.Exception is not null)
        {
            throw task.Exception;
        }

        if (task.Result.IsNotOk)
        {
            throw new PluginException("Sending listens failed");
        }
    }

    /// <inheritdoc />
    /// <exception cref="PluginException">Sending listens failed.</exception>
    public async Task SendListensAsync(UserConfig config, IEnumerable<StoredListen> storedListens, CancellationToken cancellationToken)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var request = new SubmitListensRequest
        {
            ApiToken = config.PlaintextApiToken,
            ListenType = ListenType.Import,
            Payload = ToListens(storedListens),
            BaseUrl = pluginConfig.ListenBrainzApiUrl
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
        var pluginConfig = Plugin.GetConfiguration();
        var request = new ValidateTokenRequest(apiToken) { BaseUrl = pluginConfig.ListenBrainzApiUrl };
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
        return recordingMsid ?? throw new PluginException("Did not receive listen");
    }

    /// <inheritdoc />
    public async Task<string?> GetRecordingMsidByListenTsAsync(UserConfig config, long ts)
    {
        var userName = config.UserName;
        if (string.IsNullOrEmpty(userName))
        {
            // Earlier 3.x plugin configurations did not store the username
            _logger.LogDebug("ListenBrainz username is not available, getting it via token validation");
            userName = await GetListenBrainzUsernameAsync(config.PlaintextApiToken);
        }

        var request = new GetUserListensRequest(userName);
        var response = await _apiClient.GetUserListens(request, CancellationToken.None);
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

    /// <summary>
    /// Fetch ListenBrainz username using the API token.
    /// </summary>
    /// <param name="userApiToken">ListenBrainz API token.</param>
    /// <returns>ListenBrainz username associated with the API token.</returns>
    /// <exception cref="PluginException">Username could not be obtained.</exception>
    private string GetListenBrainzUsername(string userApiToken)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var tokenRequest = new ValidateTokenRequest(userApiToken) { BaseUrl = pluginConfig.ListenBrainzApiUrl };
        var task = _apiClient.ValidateToken(tokenRequest, CancellationToken.None);
        task.Wait();
        if (task.Exception is not null)
        {
            throw task.Exception;
        }

        if (task.Result.UserName is null)
        {
            throw new PluginException("No username received");
        }

        return task.Result.UserName;
    }

    /// <summary>
    /// Fetch ListenBrainz username using the API token.
    /// </summary>
    /// <param name="userApiToken">ListenBrainz API token.</param>
    /// <returns>ListenBrainz username associated with the API token.</returns>
    /// <exception cref="PluginException">Username could not be obtained.</exception>
    private async Task<string> GetListenBrainzUsernameAsync(string userApiToken)
    {
        var pluginConfig = Plugin.GetConfiguration();
        var tokenRequest = new ValidateTokenRequest(userApiToken) { BaseUrl = pluginConfig.ListenBrainzApiUrl };
        var tokenResponse = await _apiClient.ValidateToken(tokenRequest, CancellationToken.None);
        if (tokenResponse.UserName is null)
        {
            throw new PluginException("ValidateToken response does not contain username");
        }

        return tokenResponse.UserName;
    }
}
