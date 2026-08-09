using Jellyfin.Plugin.ListenBrainz.Api.Models;

namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncGeneratedPlaylists;

internal sealed record PlaylistCandidate(Playlist Playlist, PlaylistType Type);
