namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// A ListenBrainz playlist selected for syncing by the discovery stage.
/// </summary>
/// <param name="ListenBrainzPlaylistId">ListenBrainz playlist ID (MBID).</param>
/// <param name="Origin">Origin of the playlist.</param>
/// <param name="GeneratedType">
/// Generated playlist type. Set only if <paramref name="Origin"/> is <see cref="PlaylistOrigin.Generated"/>.
/// </param>
/// <param name="Title">ListenBrainz playlist title.</param>
/// <param name="CreatedAt">ListenBrainz playlist creation date.</param>
public sealed record DiscoveredPlaylist(
    string ListenBrainzPlaylistId,
    PlaylistOrigin Origin,
    string? GeneratedType,
    string Title,
    DateTime CreatedAt);
