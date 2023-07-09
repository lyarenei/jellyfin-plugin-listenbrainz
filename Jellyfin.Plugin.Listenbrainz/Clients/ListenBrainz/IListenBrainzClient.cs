using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Plugin.Listenbrainz.Models;
using Jellyfin.Plugin.Listenbrainz.Models.Listenbrainz;
using Jellyfin.Plugin.Listenbrainz.Resources.ListenBrainz;

namespace Jellyfin.Plugin.Listenbrainz.Clients.ListenBrainz;

/// <summary>
/// ListenBrainz client.
/// </summary>
public interface IListenBrainzClient
{
    /// <summary>
    /// Submit a listen for specified user.
    /// </summary>
    /// <param name="user">ListenBrainz user.</param>
    /// <param name="listen">Listen to submit.</param>
    /// <returns>Task representing async operation.</returns>
    public Task SubmitListen(LbUser user, Listen listen);

    /// <summary>
    /// Submit listens for specified user.
    /// </summary>
    /// <param name="user">ListenBrainz user.</param>
    /// <param name="listens">Listens to submit.</param>
    /// <returns>Task representing async operation.</returns>
    public Task SubmitListens(LbUser user, IEnumerable<Listen> listens);

    /// <summary>
    /// Submit a 'now playing' listen for specified user.
    /// </summary>
    /// <param name="user">ListenBrainz user.</param>
    /// <param name="listen">Listen to submit as 'now playing'.</param>
    /// <returns>Task representing async operation.</returns>
    public Task SubmitNowPlaying(LbUser user, Listen listen);

    /// <summary>
    /// Submit a listen feedback for specified user.
    /// </summary>
    /// <param name="user">ListenBrainz user.</param>
    /// <param name="listen">Listen to submit teh feedback for.</param>
    /// <param name="isFavorite">Listened track is favorite.</param>
    /// <returns>Task representing async operation.</returns>
    public Task SubmitFeedback(LbUser user, Listen listen, bool isFavorite);

    /// <summary>
    /// Validate API token for specified user.
    /// </summary>
    /// <param name="user">ListenBrainz user.</param>
    /// <returns>Task representing async operation.</returns>
    public Task ValidateToken(LbUser user);

    /// <summary>
    /// Get listens of specified user.
    /// </summary>
    /// <param name="user">ListenBrainz user.</param>
    /// <param name="limit">Maximum listens to fetch. Defaults to <see cref="Limits.MaxListensToGet"/>.</param>
    /// <returns>Task representing async operation, returning received listens.</returns>
    public Task<IEnumerable<Listen>> GetUserListens(LbUser user, int limit = Limits.MaxListensToGet);

    /// <summary>
    /// Get a listen at a specified timestamp.
    /// </summary>
    /// <param name="user">ListenBrainz user.</param>
    /// <param name="timestamp">Listen timestamp.</param>
    /// <returns>Task representing async operation, returning a listen.</returns>
    public Task<Listen?> GetListen(LbUser user, long timestamp);
}
