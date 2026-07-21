using Jellyfin.Plugin.ListenBrainz.Configuration;

namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncGeneratedPlaylists;

/// <summary>
/// Describes a <see cref="PlaylistType"/>: how to recognize it, how it is retained,
/// and whether a user has it enabled.
/// </summary>
/// <param name="Type">The playlist type.</param>
/// <param name="Retention">The retention strategy for the type.</param>
/// <param name="SourcePatchPrefix">The ListenBrainz source patch identifying the type.</param>
/// <param name="IsEnabled">Whether the type is enabled for a given user.</param>
internal sealed record PlaylistTypeDescriptor(
    PlaylistType Type,
    PlaylistRetention Retention,
    string SourcePatchPrefix,
    Func<UserConfig, bool> IsEnabled);
