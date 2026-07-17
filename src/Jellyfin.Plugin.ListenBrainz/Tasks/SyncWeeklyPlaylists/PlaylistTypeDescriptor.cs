using Jellyfin.Plugin.ListenBrainz.Configuration;

namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncWeeklyPlaylists;

/// <summary>
/// Describes a <see cref="PlaylistType"/>: how to recognize it, how it is retained,
/// and whether a user has it enabled.
/// </summary>
/// <param name="Type">The playlist type.</param>
/// <param name="Retention">The retention strategy for the type.</param>
/// <param name="SourcePatch">The ListenBrainz source patch identifying the type.</param>
/// <param name="IsEnabled">Whether the type is enabled for a given user.</param>
internal sealed record PlaylistTypeDescriptor(
    PlaylistType Type,
    PlaylistRetention Retention,
    string SourcePatch,
    Func<UserConfig, bool> IsEnabled);
