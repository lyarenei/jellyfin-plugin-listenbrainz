namespace Jellyfin.Plugin.ListenBrainz.Api.Models.Responses;

/// <summary>
/// Helper class for parsing ListenBrainz playlists.
/// </summary>
internal class WrappedPlaylist
{
    internal WrappedPlaylist()
    {
        Playlist = new();
    }

    public Playlist Playlist { get; set; }
}
