using Jellyfin.Plugin.ListenBrainz.Common.Extensions;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Dtos;
using Playlist = Jellyfin.Plugin.ListenBrainz.Api.Models.Playlist;

namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncWeeklyPlaylists;

/// <summary>
/// Selection and classification utils for ListenBrainz weekly rotation playlists.
/// </summary>
internal static class WeeklyRotationPolicy
{
    /// <summary>
    /// Number of playlists kept per family (current and previous week).
    /// </summary>
    private const int RotationPlaylistCount = 2;

    /// <summary>
    /// Classifies a ListenBrainz playlist source patch into a weekly playlist family.
    /// </summary>
    /// <param name="sourcePatch">The playlist source patch.</param>
    /// <returns>The matching weekly playlist family, or null if the patch is not a weekly rotation.</returns>
    internal static WeeklyPlaylistType? ClassifyBySourcePatch(string? sourcePatch)
    {
        return sourcePatch switch
        {
            "weekly-jams" => WeeklyPlaylistType.Jams,
            "weekly-exploration" => WeeklyPlaylistType.Exploration,
            _ => null,
        };
    }

    /// <summary>
    /// Gets the persisted category discriminator for a weekly playlist family.
    /// Inverse of <see cref="TryGetWeeklyType"/>.
    /// </summary>
    /// <param name="type">The weekly playlist family.</param>
    /// <returns>The category discriminator stored on a mapping.</returns>
    internal static string CategoryFor(WeeklyPlaylistType type) => type.ToString();

    /// <summary>
    /// Pick the created-for playlists (current and previous) rotation for each type the user has enabled.
    /// </summary>
    /// <remarks>
    /// ListenBrainz does not provide "current weekly jams" alias, so newest <see cref="Playlist.CreatedAt"/>
    /// is treated as the current week and the next one as the last week one.
    /// </remarks>
    /// <param name="playlists">Playlists created for the user.</param>
    /// <param name="userConfig">User configuration.</param>
    /// <returns>The weekly playlists matching the user settings.</returns>
    internal static IEnumerable<WeeklyPlaylistCandidate> PickWeeklyRotationPlaylists(
        IEnumerable<Playlist> playlists,
        UserConfig userConfig)
    {
        return playlists
            .Select(GetWeeklyPlaylistCandidate)
            .WhereNotNull()
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Playlist.PlaylistId))
            .Where(candidate => IsPlaylistTypeEnabled(userConfig, candidate.Type))
            .GroupBy(candidate => candidate.Type)
            .SelectMany(group => group
                .OrderByDescending(candidate => candidate.Playlist.CreatedAt)
                .ThenByDescending(candidate => candidate.Playlist.Identifier, StringComparer.OrdinalIgnoreCase)
                .Take(RotationPlaylistCount))
            .OrderBy(candidate => candidate.Type)
            .ThenByDescending(candidate => candidate.Playlist.CreatedAt);
    }

    /// <summary>
    /// Determines whether a persisted mapping should be pruned given the current rotation.
    /// </summary>
    /// <param name="mapping">The persisted playlist mapping.</param>
    /// <param name="rotationIds">ListenBrainz playlist IDs currently in rotation.</param>
    /// <param name="rotationTypes">Weekly playlist families currently in rotation.</param>
    /// <returns>True if the mapping is owned by the weekly task and no longer in rotation.</returns>
    internal static bool ShouldPruneMapping(
        PlaylistMapping mapping,
        HashSet<string> rotationIds,
        HashSet<WeeklyPlaylistType> rotationTypes)
    {
        if (!TryGetWeeklyType(mapping.Category, out var type))
        {
            return false;
        }

        return rotationTypes.Contains(type) && !rotationIds.Contains(mapping.ListenBrainzPlaylistId);
    }

    private static WeeklyPlaylistCandidate? GetWeeklyPlaylistCandidate(Playlist playlist)
    {
        var type = ClassifyBySourcePatch(playlist.JspfPlaylist.SourcePatch);
        return type is null ? null : new WeeklyPlaylistCandidate(playlist, type.Value);
    }

    private static bool IsPlaylistTypeEnabled(UserConfig userConfig, WeeklyPlaylistType playlistType)
    {
        return playlistType switch
        {
            WeeklyPlaylistType.Jams => userConfig.IsWeeklyJamsSyncEnabled,
            WeeklyPlaylistType.Exploration => userConfig.IsWeeklyExplorationSyncEnabled,
            _ => false,
        };
    }

    private static bool TryGetWeeklyType(string? category, out WeeklyPlaylistType type)
    {
        return Enum.TryParse(category, ignoreCase: true, out type) && Enum.IsDefined(type);
    }
}
