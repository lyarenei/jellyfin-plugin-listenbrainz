# How the plugin works

This document describes what the plugin features actually do. The plugin configuration is available in the
[configuration](configuration.md) document.

## Sending listens

Submitting listens is the main job of the plugin. ListenBrainz accepts two kinds of listen — a `now playing`
listen and a normal one — the plugin sends both.

### Sending a 'now playing' listen

A `now playing` listen has no conditions attached to it. The plugin sends one as soon as the server reports the
start of a playback with a `PlaybackStart` event, provided that:

- the user has set an API token,
- the user has listen submission enabled,
- the track is in an allowed library, and
- the track has at least an artist and a title.

If the MusicBrainz integration is enabled, the additional metadata is fetched first and included in the listen.

A failed `now playing` listen is never cached or retried by the plugin (these are ephemeral).

### Sending a listen

A normal listen has to meet the conditions listed above plus two more:

- the playback time is at least 4 minutes, or at least 50% of the track runtime, and
- the track has a recording MBID, if the user has strict mode enabled.

Which event starts the process depends on the configuration — either `PlaybackStopped` or `UserDataSaved`. Both
modes are described under
[use alternative event for recognizing listens](configuration.md#use-alternative-event-for-recognizing-listens).

From there the plugin:

1. fetches the additional metadata from MusicBrainz, if the integration is enabled,
2. writes the listen to a backup file, if backups are enabled,
3. sends the listen to ListenBrainz, and
4. sends the favorite status of the track, if favorite sync is enabled.

## Listen cache

Listens that cannot be sent are kept in a cache so they are not lost. The cache lives in the plugin data
directory, at `<jellyfin config>/plugins/configurations/ListenBrainz/cache.json`.

The `Resubmit listens` task sends them again. It runs every 24 to 25 hours, with the exact interval picked at
each server start, and you can also run it manually from the server administration. A listen leaves the cache only
once ListenBrainz has accepted it. Favorites are not synchronized during this task.

Nothing is cached for users without a valid configuration, or for users who have listen submission disabled.

## Syncing favorites

The plugin marks favorite Jellyfin tracks as loved recordings in ListenBrainz, and loved ListenBrainz recordings
as favorite Jellyfin tracks.

Albums and artists are not supported, since ListenBrainz has no concept of a favorite album or artist. Hated
recordings are not supported as well, since Jellyfin has no equivalent.

Both directions need a recording MBID, taken from the Jellyfin metadata of the track or from MusicBrainz when the
[MusicBrainz integration](configuration.md#fetch-additional-metadata-from-musicbrainz) is enabled.

#### From Jellyfin to ListenBrainz

The favorite status is set right after the plugin submits a listen of that track. With
[immediate favorite sync](configuration.md#immediate-favorite-sync) it is also sent the moment you change the
status in Jellyfin. The ListenBrainz interface may take a while to catch up and show the change.

#### From ListenBrainz to Jellyfin

This direction only exists as the `Sync loved tracks` task, which can be run manually from the scheduled tasks in
the server administration. It is not scheduled as most of the users will run it rarely and it can take a long time with
large amounts of loved recordings on a very large libraries in Jellyfin.

The task collects the loved recordings of every user with favorite sync enabled, walks through all tracks in the
allowed libraries that carry a recording MBID or a track MBID, and marks the loved ones as favorite. It never
removes a favorite mark.

## Syncing playlists

ListenBrainz generates playlists for each of its users, and the plugin copies them into Jellyfin for anyone with
[generated playlist sync](configuration.md#enable-generated-playlist-sync) enabled. For now, only the direction from ListenBrainz to Jellyfin is supported.

As of now, four playlist types are supported:

| Playlist type                     | ListenBrainz generates it | The plugin keeps             |
|-----------------------------------|---------------------------|------------------------------|
| Weekly jams                       | every Monday              | the current and the previous |
| Weekly exploration                | every Monday              | the current and the previous |
| Top discoveries of `<year>`       | once a year               | all of them                  |
| Top missed recordings of `<year>` | once a year               | all of them                  |

The user can pick the types in the configuration. Everything else, including empty playlists, is ignored.

The work is done by the `Sync generated playlists from ListenBrainz` task, which runs every Monday at a random
minute within the first hour of the day. The minute is chosen at each server start to spread the load on the
ListenBrainz servers. You can also run the task manually at any time.

### How the plugin creates the playlists

A synchronized playlist keeps the name it has on ListenBrainz. The plugin tags it with `ListenBrainz` and makes
the selected user its owner.

Which Jellyfin playlist belongs to which ListenBrainz playlist is recorded in
`<jellyfin config>/plugins/configurations/ListenBrainz/playlist-sync-state.json`.

For each playlist in a run, one of three things happens:

- ListenBrainz has not regenerated the playlist since the last sync, so the Jellyfin playlist is left alone.
- The plugin has a record of the playlist, so the tracks in the matching Jellyfin playlist are replaced.
- The plugin has no record, so it looks for a Jellyfin playlist with the same name and the `ListenBrainz` tag,
  and either replaces the tracks in it or creates a new playlist.

If not a single track of a playlist can be matched in the library, no Jellyfin playlist is created or changed.

Once a weekly playlist rotates out of the selection, the plugin deletes the Jellyfin playlist along with its
record. With [keep playlists after rotation](configuration.md#keep-playlists-after-rotation) the playlist stays
and only the record is dropped, after which the plugin ignores that playlist.

### Track matching

For every track in a ListenBrainz playlist, the plugin has to find the counterpart in the Jellyfin library. Since
ListenBrainz accepts listens without a recording MBID, playlists may contain tracks that do not have it, so the
plugin works through several methods in order, from the most to the least reliable:

1. **Recording MBID** — the best case: a track with exactly this recording MBID.
2. **Album MBID and title** — a track with the same album MBID and the same title, ignoring case.
3. **Related recordings** — the related recordings from MusicBrainz, such as other versions of the same song,
   matched against the recording MBIDs in the library. Needs the MusicBrainz integration.
4. **Artist and title** — a library search for the title, keeping tracks whose artist appears in the artist
   credit of the ListenBrainz track.
5. **Album name and title** — a library search for the title, keeping tracks with the same album name, ignoring
   case.

Searching by title alone is deliberately not attempted, as it produces far too many false matches.

When every method comes up empty, the track is either missing from your library or present with too little
metadata to recognize it.

Recording MBIDs in your own metadata give by far the best results. Unlike the other features, this one will not
fall back to MusicBrainz to fill in a recording MBID.

## Scheduled tasks

The plugin adds the following tasks to the server administration, all under the `ListenBrainz` category.

| Task                                          | Default schedule     | Purpose                                                    |
|-----------------------------------------------|----------------------|------------------------------------------------------------|
| Resubmit listens                              | every 24 to 25 hours | Sends the listens sitting in the listen cache again.       |
| Sync generated playlists from ListenBrainz    | every Monday         | Copies the generated ListenBrainz playlists into Jellyfin. |
| Sync loved tracks                             | manual               | Marks loved ListenBrainz recordings as Jellyfin favorites. |
| Sync playlists from ListenBrainz (deprecated) | manual               | Does nothing; removed in a later version.                  |
