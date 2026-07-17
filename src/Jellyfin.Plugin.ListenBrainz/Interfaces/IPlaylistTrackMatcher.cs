using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Plugin.ListenBrainz.Api.Models;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// Matches ListenBrainz playlist tracks to Jellyfin library items.
/// </summary>
public interface IPlaylistTrackMatcher
{
    /// <summary>
    /// Gets the candidate audio items from the allowed libraries for the given user.
    /// </summary>
    /// <param name="user">The user to resolve candidates for.</param>
    /// <returns>The candidate audio items.</returns>
    IReadOnlyList<BaseItem> GetCandidateAudioItems(User user);

    /// <summary>
    /// Finds the best matching Jellyfin item for a ListenBrainz playlist track.
    /// </summary>
    /// <param name="candidates">Candidate audio items, as returned by <see cref="GetCandidateAudioItems"/>.</param>
    /// <param name="user">The user the track is matched for.</param>
    /// <param name="track">The ListenBrainz playlist track.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching item. Null if no match.</returns>
    Task<BaseItem?> FindMatchAsync(
        IReadOnlyList<BaseItem> candidates,
        User user,
        PlaylistTrack track,
        CancellationToken cancellationToken);
}
