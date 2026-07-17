using Jellyfin.Plugin.ListenBrainz.Api.Models;

namespace Jellyfin.Plugin.ListenBrainz.Tasks.SyncWeeklyPlaylists;

internal sealed record WeeklyPlaylistCandidate(Playlist Playlist, PlaylistType Type);
