using Jellyfin.Plugin.ListenBrainz.Api.Models;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

internal sealed record WeeklyPlaylistCandidate(Playlist Playlist, WeeklyPlaylistType Type);
