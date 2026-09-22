using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Playlist = Jellyfin.Plugin.ListenBrainz.Api.Models.Playlist;

namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncGeneratedPlaylists;

/// <summary>
/// Selection, classification and retention rules for ListenBrainz generated playlists.
/// </summary>
internal static class PlaylistTypePolicy
{
    /// <summary>
    /// Descriptors of all known playlist types. A null keep limit means the type is never pruned.
    /// </summary>
    private static readonly IReadOnlyDictionary<PlaylistType, PlaylistTypeDescriptor> _descriptors =
        new PlaylistTypeDescriptor[]
        {
            new(
                PlaylistType.Jams,
                KeepNewest: 2,
                "weekly-jams",
                uc => uc.IsWeeklyJamsSyncEnabled),
            new(
                PlaylistType.Exploration,
                KeepNewest: 2,
                "weekly-exploration",
                uc => uc.IsWeeklyExplorationSyncEnabled),
            new(
                PlaylistType.TopDiscoveries,
                KeepNewest: null,
                "top-discoveries-of",
                uc => uc.IsTopDiscoveriesSyncEnabled),
            new(
                PlaylistType.TopMissedRecordings,
                KeepNewest: null,
                "top-missed-recordings-of",
                uc => uc.IsTopMissedRecordingsSyncEnabled),
        }.ToDictionary(d => d.Type);

    /// <summary>
    /// Classifies a ListenBrainz playlist source patch into a playlist type.
    /// </summary>
    /// <param name="sourcePatch">The playlist source patch.</param>
    /// <returns>The matching playlist type, or null if the patch is not a known type.</returns>
    internal static PlaylistType? ClassifyBySourcePatch(string? sourcePatch)
    {
        return DescriptorForPatch(sourcePatch)?.Type;
    }

    /// <summary>
    /// Gets the persisted discriminator of a playlist type. Inverse of <see cref="ParsePlaylistType"/>.
    /// </summary>
    /// <param name="type">The playlist type.</param>
    /// <returns>The discriminator stored on an entry.</returns>
    internal static string CategoryFor(PlaylistType type) => type.ToString();

    /// <summary>
    /// Gets the playlist type of a persisted discriminator. Inverse of <see cref="CategoryFor"/>.
    /// </summary>
    /// <param name="generatedType">The persisted discriminator.</param>
    /// <returns>The playlist type, or null if the discriminator is not a known type.</returns>
    internal static PlaylistType? ParsePlaylistType(string? generatedType)
    {
        return TryGetPlaylistType(generatedType, out var type) ? type : null;
    }

    /// <summary>
    /// Picks the playlists to sync for the types a user has enabled.
    /// </summary>
    /// <remarks>
    /// ListenBrainz does not mark the current playlist, so the newest
    /// <see cref="Playlist.CreatedAt"/> is treated as the current one.
    /// </remarks>
    /// <param name="playlists">Playlists created for the user.</param>
    /// <param name="userConfig">User configuration.</param>
    /// <returns>The playlists matching the user settings.</returns>
    internal static IEnumerable<PlaylistCandidate> SelectPlaylists(
        IEnumerable<Playlist> playlists,
        UserConfig userConfig)
    {
        return playlists
            .Select(GetPlaylistCandidate)
            .WhereNotNull()
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Playlist.PlaylistId))
            .Where(candidate => _descriptors[candidate.Type].IsEnabled(userConfig))
            .GroupBy(candidate => candidate.Type)
            .SelectMany(TakeForType)
            .OrderBy(candidate => candidate.Type) // Ensure stable order
            .ThenByDescending(candidate => candidate.Playlist.CreatedAt);
    }

    /// <summary>
    /// Determines whether a persisted entry still matches the listed playlist.
    /// </summary>
    /// <param name="entry">The persisted playlist sync entry.</param>
    /// <param name="playlist">The playlist metadata from the listing.</param>
    /// <returns>True if the playlist has not been regenerated since the last sync.</returns>
    internal static bool IsUpToDate(PlaylistSyncEntry entry, Playlist playlist)
    {
        return entry.CreatedAt == playlist.CreatedAt;
    }

    /// <summary>
    /// Determines whether a persisted entry should be pruned given the current selection.
    /// </summary>
    /// <param name="entry">The persisted playlist sync entry.</param>
    /// <param name="selectedPlaylistIds">ListenBrainz playlist IDs selected this run.</param>
    /// <param name="syncedTypes">Playlist types that were fully synced this run.</param>
    /// <returns>True if the entry belongs to a capped type and is no longer in the selection.</returns>
    internal static bool ShouldPruneEntry(
        PlaylistSyncEntry entry,
        HashSet<string> selectedPlaylistIds,
        HashSet<PlaylistType> syncedTypes)
    {
        // The store is shared across sync tasks; leave entries this task does not own.
        if (entry.Origin != PlaylistOrigin.Generated)
        {
            return false;
        }

        if (!TryGetPlaylistType(entry.GeneratedType, out var type))
        {
            return false;
        }

        if (_descriptors[type].KeepNewest is null)
        {
            return false;
        }

        // Prune only after a clean sync, otherwise the user could be left with nothing.
        return syncedTypes.Contains(type) && !selectedPlaylistIds.Contains(entry.ListenBrainzPlaylistId);
    }

    private static IEnumerable<PlaylistCandidate> TakeForType(IGrouping<PlaylistType, PlaylistCandidate> group)
    {
        var ordered = group
            .OrderByDescending(candidate => candidate.Playlist.CreatedAt)
            .ThenByDescending(candidate => candidate.Playlist.Identifier, StringComparer.OrdinalIgnoreCase);

        return _descriptors[group.Key].KeepNewest is int keep ? ordered.Take(keep) : ordered;
    }

    private static PlaylistCandidate? GetPlaylistCandidate(Playlist playlist)
    {
        var type = ClassifyBySourcePatch(playlist.JspfPlaylist.SourcePatch);
        return type is null ? null : new PlaylistCandidate(playlist, type.Value);
    }

    private static bool TryGetPlaylistType(string? category, out PlaylistType type)
    {
        return Enum.TryParse(category, ignoreCase: true, out type) && Enum.IsDefined(type);
    }

    private static PlaylistTypeDescriptor? DescriptorForPatch(string? sourcePatch)
    {
        if (string.IsNullOrEmpty(sourcePatch))
        {
            return null;
        }

        return _descriptors.Values.FirstOrDefault(d => MatchesPatch(d.SourcePatchPrefix, sourcePatch));
    }

    private static bool MatchesPatch(string prefix, string sourcePatch)
    {
        return sourcePatch.Equals(prefix, StringComparison.Ordinal) ||
               sourcePatch.StartsWith(prefix + "-", StringComparison.Ordinal);
    }
}
