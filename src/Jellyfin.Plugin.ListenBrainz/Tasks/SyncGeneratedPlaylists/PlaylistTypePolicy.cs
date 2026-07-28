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
    /// Number of playlists kept per rotation type (current and previous).
    /// </summary>
    private const int RotationPlaylistCount = 2;

    /// <summary>
    /// Descriptors for every known playlist type, keyed by <see cref="PlaylistType"/>.
    /// </summary>
    private static readonly IReadOnlyDictionary<PlaylistType, PlaylistTypeDescriptor> _descriptors =
        new PlaylistTypeDescriptor[]
        {
            new(
                PlaylistType.Jams,
                PlaylistRetention.Rotation,
                "weekly-jams",
                uc => uc.IsWeeklyJamsSyncEnabled),
            new(
                PlaylistType.Exploration,
                PlaylistRetention.Rotation,
                "weekly-exploration",
                uc => uc.IsWeeklyExplorationSyncEnabled),
            new(
                PlaylistType.TopDiscoveries,
                PlaylistRetention.Archive,
                "top-discoveries-of",
                uc => uc.IsTopDiscoveriesSyncEnabled),
            new(
                PlaylistType.TopMissedRecordings,
                PlaylistRetention.Archive,
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
    /// Gets the persisted category discriminator for a playlist type.
    /// Inverse of <see cref="TryGetPlaylistType"/>.
    /// </summary>
    /// <param name="type">The playlist type.</param>
    /// <returns>The category discriminator stored on a mapping.</returns>
    internal static string CategoryFor(PlaylistType type) => type.ToString();

    /// <summary>
    /// Picks the playlists to sync for the types a user has enabled.
    /// </summary>
    /// <remarks>
    /// Rotation types keep the current and previous playlists (ListenBrainz does not provide a "current"
    /// alias, so the newest <see cref="Playlist.CreatedAt"/> is treated as the current one).
    /// Archive types keep every playlist.
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
            .Where(candidate => IsPlaylistTypeEnabled(userConfig, candidate.Type))
            .GroupBy(candidate => candidate.Type)
            .SelectMany(TakeForType)
            .OrderBy(candidate => candidate.Type)
            .ThenByDescending(candidate => candidate.Playlist.CreatedAt);
    }

    /// <summary>
    /// Determines whether a persisted mapping is already up to date with the playlist from the listing,
    /// i.e. the playlist has not been regenerated since it was last synced.
    /// </summary>
    /// <param name="mapping">The persisted playlist mapping.</param>
    /// <param name="playlist">The playlist metadata from the created-for listing.</param>
    /// <returns>True if the mapping already reflects the current playlist.</returns>
    internal static bool IsUpToDate(PlaylistMapping mapping, Playlist playlist)
    {
        return mapping.CreatedAt == playlist.CreatedAt;
    }

    /// <summary>
    /// Determines whether a persisted mapping should be pruned given the current selection.
    /// </summary>
    /// <param name="mapping">The persisted playlist mapping.</param>
    /// <param name="selectedPlaylistIds">ListenBrainz playlist IDs selected this run.</param>
    /// <param name="syncedTypes">Playlist types that were fully synced this run.</param>
    /// <returns>True if the mapping is owned by a rotation type and no longer in rotation.</returns>
    internal static bool ShouldPruneMapping(
        PlaylistMapping mapping,
        HashSet<string> selectedPlaylistIds,
        HashSet<PlaylistType> syncedTypes)
    {
        if (!TryGetPlaylistType(mapping.Category, out var type))
        {
            return false;
        }

        // Archive types are permanent and never pruned.
        if (RetentionOf(type) != PlaylistRetention.Rotation)
        {
            return false;
        }

        return syncedTypes.Contains(type) && !selectedPlaylistIds.Contains(mapping.ListenBrainzPlaylistId);
    }

    private static IEnumerable<PlaylistCandidate> TakeForType(IGrouping<PlaylistType, PlaylistCandidate> group)
    {
        var ordered = group
            .OrderByDescending(candidate => candidate.Playlist.CreatedAt)
            .ThenByDescending(candidate => candidate.Playlist.Identifier, StringComparer.OrdinalIgnoreCase);

        return RetentionOf(group.Key) == PlaylistRetention.Rotation
            ? ordered.Take(RotationPlaylistCount)
            : ordered;
    }

    private static PlaylistCandidate? GetPlaylistCandidate(Playlist playlist)
    {
        var type = ClassifyBySourcePatch(playlist.JspfPlaylist.SourcePatch);
        return type is null ? null : new PlaylistCandidate(playlist, type.Value);
    }

    private static bool IsPlaylistTypeEnabled(UserConfig userConfig, PlaylistType playlistType)
    {
        return DescriptorForType(playlistType).IsEnabled(userConfig);
    }

    private static PlaylistRetention RetentionOf(PlaylistType type) => DescriptorForType(type).Retention;

    private static bool TryGetPlaylistType(string? category, out PlaylistType type)
    {
        return Enum.TryParse(category, ignoreCase: true, out type) && Enum.IsDefined(type);
    }

    private static PlaylistTypeDescriptor DescriptorForType(PlaylistType type)
    {
        return _descriptors[type];
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
        // Match exactly or as a prefix
        return sourcePatch.Equals(prefix, StringComparison.Ordinal) ||
               sourcePatch.StartsWith(prefix + "-", StringComparison.Ordinal);
    }
}
