namespace Jellyfin.Plugin.ListenBrainz.Dtos;

/// <summary>
/// Where a ListenBrainz playlist came from.
/// </summary>
public enum PlaylistOrigin
{
    /// <summary>
    /// Playlist generated for the user by ListenBrainz. Its ID rotates on every regeneration.
    /// </summary>
    Generated = 0,

    /// <summary>
    /// Playlist created by the user.
    /// </summary>
    UserCreated = 1,

    /// <summary>
    /// Playlist the user collaborates on.
    /// </summary>
    Collaborative = 2,
}
