using Jellyfin.Plugin.ListenBrainz.Api.Models;

namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncWeeklyPlaylists;

internal sealed record PlaylistCandidate(Playlist Playlist, PlaylistType Type);
