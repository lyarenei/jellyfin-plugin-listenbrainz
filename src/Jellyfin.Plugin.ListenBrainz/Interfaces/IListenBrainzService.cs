using Jellyfin.Plugin.ListenBrainz.Api.Models;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using MediaBrowser.Controller.Entities.Audio;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// ListenBrainz service.
/// </summary>
public interface IListenBrainzService
{
    /// <summary>
    /// Send 'now playing' listen of specified item.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="item">Audio item currently being listened to.</param>
    /// <param name="audioMetadata">Additional metadata for this audio item.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success.</returns>
    public Task<bool> SendNowPlayingAsync(
        UserConfig config,
        Audio item,
        AudioItemMetadata? audioMetadata,
        CancellationToken cancellationToken);

    /// <summary>
    /// Send a single listen of specified item.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="item">Audio item of the listen.</param>
    /// <param name="metadata">Additional metadata for this audio item.</param>
    /// <param name="listenedAt">Timestamp of the listen.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success.</returns>
    public Task<bool> SendListenAsync(
        UserConfig config,
        Audio item,
        AudioItemMetadata? metadata,
        long listenedAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Send a single listen.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="listen">Listen to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success.</returns>
    public Task<bool> SendListenAsync(UserConfig config, Listen listen, CancellationToken cancellationToken);

    /// <summary>
    /// Send a feedback for a specific recording, identified by either a MBID or MSID.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="isFavorite">The recording is marked as favorite.</param>
    /// <param name="recordingMbid">MusicBrainz ID identifying the recording.</param>
    /// <param name="recordingMsid">MessyBrainz ID identifying the recording.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success.</returns>
    public Task<bool> SendFeedbackAsync(
        UserConfig config,
        bool isFavorite,
        string? recordingMbid = null,
        string? recordingMsid = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send multiple listens ('import') to ListenBrainz.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="listens">Listens to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success.</returns>
    public Task<bool> SendListensAsync(
        UserConfig config,
        IEnumerable<Listen> listens,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validate specified API token.
    /// </summary>
    /// <param name="apiToken">Token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Token validation data.</returns>
    public Task<ValidatedToken> ValidateTokenAsync(string apiToken, CancellationToken cancellationToken);

    /// <summary>
    /// Get a recording MSID (MessyBrainz ID) associated with a listen submitted to ListenBrainz.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="ts">Timestamp of the submitted listen.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Recording MSID associated with a specified listen timestamp.</returns>
    public Task<string> GetRecordingMsidByListenTsAsync(
        UserConfig config,
        long ts,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get a collection of recording MBIDs which are loved by the user.
    /// </summary>
    /// <param name="config">ListenBrainz user configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recording MBIDs identifying loved tracks by the user.</returns>
    public Task<IEnumerable<string>> GetLovedTracksAsync(UserConfig config, CancellationToken cancellationToken);
}
