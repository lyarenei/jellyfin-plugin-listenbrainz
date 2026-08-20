# Plugin configuration

This document describes every plugin setting. For a description of the features themselves, see
[how it works](how-it-works.md).

Only the server administrator can change these settings, because Jellyfin does not support per-user plugin
settings. This means that the server administrator has access to all ListenBrainz API tokens.

The configuration page has five tabs, each with its own `Save` button that saves only that tab:

- [User config](#user-config)
- [General](#general)
- [MusicBrainz](#musicbrainz)
- [Backup](#backup)
- [Libraries](#libraries)

## User config

Every user who wants to send data to ListenBrainz needs their own configuration. Select the user in the
`Select Jellyfin user` list first, then change their settings and save when you are done.

##### ListenBrainz API token

The API token of the ListenBrainz user, available from the
[ListenBrainz user profile](https://listenbrainz.org/profile/). Without it, the plugin can neither send nor
receive data.

Click `Check` to verify the token — the plugin then shows the ListenBrainz username it belongs to. Saving the
configuration verifies the token as well, and stores that username for later use.

The token is obfuscated in the plugin configuration file, but not encrypted.

##### Enable submitting listens

Enables sending listens of the selected user to ListenBrainz.

##### Enable syncing favorites

Marks favorite Jellyfin tracks as loved recordings in ListenBrainz. If you remove the favorite mark, the recording
goes back to neutral. The `Sync loved tracks` task handles the opposite direction.

Albums, artists, and hated recordings are not synchronized: ListenBrainz has no concept of a favorite album or
artist, and Jellyfin has no concept of a hated track.

##### Enable playlist sync (deprecated)

This setting no longer does anything and will be removed in a future version. Use
[Enable generated playlist sync](#enable-generated-playlist-sync) instead.

##### Enable generated playlist sync

Enable syncing of the playlists that ListenBrainz generates for the selected user into Jellyfin. Choose which types
with the settings below; by default that is `Weekly jams` and `Top discoveries of <year>`. For the full process, see
[syncing playlists](how-it-works.md#syncing-playlists).

##### Sync Weekly Jams

Enable syncing of the `Weekly jams` playlists. ListenBrainz generates a new one every week and keeps the one from
previous week as well. Older playlists are permanently deleted. By default, the plugin behaves the same, but this can be
changed with [Keep playlists after rotation](#keep-playlists-after-rotation) option.

##### Sync Weekly Exploration

Enable syncing of the `Weekly exploration` playlists. Playlist rotation is the same as described in
[Sync Weekly Jams](#sync-weekly-jams).

These playlists contain music you have not listened to before, so your library may hold only a few of the tracks,
or none at all. These playlists can end up very small.

##### Keep playlists after rotation

Keeps a synchronized playlist in Jellyfin once it goes out of rotation, instead of deleting it. Only the weekly
playlist types rotate, so this setting has no effect on the yearly ones.

##### Sync Top Discoveries

Synchronizes the `Top discoveries of <year>` playlists. ListenBrainz generates one per year, and the plugin keeps
all of them.

##### Sync Top Missed Recordings

Synchronizes the `Top missed recordings of <year>` playlists. ListenBrainz generates one per year, and the plugin
keeps all of them.

These playlists contain music that other people listen to and you do not, so your library may hold only a few of
the tracks, or none at all. These playlists can end up very small.

##### Enable listens backup

Keep a local copy of listens for the selected user. Has no effect until the [backup path](#backup-path) is set.

##### Enable strict mode for listen submission

Submits a listen only if the track has a recording MBID, taken either from the Jellyfin metadata or from
MusicBrainz when the [MusicBrainz integration](#fetch-additional-metadata-from-musicbrainz) is enabled.

A listen that fails this check goes into the listen cache rather than being discarded. The `Resubmit listens` task
repeats the check on every attempt, so once you add the missing metadata to the track, the listen is sent in the next run.

If backups are enabled, the listen is saved even if this validation fails.

## General

##### ListenBrainz API URL

The base URL of the ListenBrainz API. Change it if you use another ListenBrainz instance or a service with a
compatible API.

##### Use alternative event for recognizing listens

The plugin can recognize a finished playback in two different ways, and this setting switches to the alternative
one.

###### Default mode

The plugin reacts to the `PlaybackStopped` event, which the server sends when a client reports the end of a
playback. The event carries the playback position, which the plugin uses to check the ListenBrainz conditions for
a listen.

Limitations:

- The client has to report the end of the playback, which is impossible while offline.
- The playback position is an optional field, so not all clients are compatible.
- The playback position in the event is optional, and the plugin ignores events that arrive without one.

###### Alternative mode

The plugin reacts to the `UserDataSaved` event with the reason `PlaybackFinished`, which the server sends
when a client marks an item as played. In general, this mode has wider support among 3rd party clients and it also
allows playback reporting of offline playbacks.

Jellyfin never defined when an item should be marked as played (closest settings are the resume percentages), and some
clients — the web client among them — report it in the moment playback starts. The plugin works around this by tracking
the real playback position from the server's `PlaybackProgress` events instead of trusting the event alone.

Limitations:

- No playback position reporting/tracking
- Without a reported position, the plugin cannot check the playback conditions. It assumes an offline playback and
  sends the listen anyway.
- Such listen always carries the current time. If a client reports old playbacks in a batch, they all end up with
  the same timestamp.

##### Immediate favorite sync

Sends the favorite status of a track to ListenBrainz as soon as you change it in Jellyfin, rather than waiting
until the plugin submits a listen of that track. Enabled by default.

This only works for tracks that have a recording MBID, either in the Jellyfin metadata or from MusicBrainz when
the [MusicBrainz integration](#fetch-additional-metadata-from-musicbrainz) is enabled.

##### Sync all playlists from ListenBrainz (deprecated)

This setting no longer does anything and will be removed in a future version.

##### MBID delimiters

Some taggers store multiple (commonly artist) MBIDs in a single metadata field, separated by a character that isn't a
comma or any other character that would be somewhat standardized. If you have configured your tagger to use some other
separator, you can configure it here so the plugin parses the multiple MBIDS correctly. Leave empty to use the
default delimiters: `;`, `,`, `/` and the unit separator control character (`0x1F`) that some taggers use.

## MusicBrainz

##### MusicBrainz API URL

The base URL of the MusicBrainz API. Change it if you use another MusicBrainz instance or a service with a
compatible API. Server restart is required to take effect.

##### Fetch additional metadata from MusicBrainz

Looks up extra metadata for a track in MusicBrainz and adds it to the listen. The lookup needs a track MBID in the
Jellyfin metadata.

The integration is optional and enabled by default — listens are submitted with or without it.

The plugin uses the following metadata:

- **Recording MBID**

  ListenBrainz uses this MBID to link a listen to a MusicBrainz entry. Without it, the listen shows the track name
  as plain text instead of a link. ListenBrainz may still link the listen on its own, but not always correctly.

  > Jellyfin 10.11 and later store the recording MBID in the metadata database. The MBID from this lookup is used
  > only when the local metadata has none.

- **Artist credit**

  MusicBrainz supplies the full artist credit string, including the join phrases between the names. Jellyfin does
  not store those phrases and cannot reconstruct the string, so without this metadata the plugin falls back to all
  artist names separated by a comma.

  MusicBrainz usually returns the default artist names, which means customized names in your own metadata —
  transliterated names, for example — are not used.

- **ISRC**

  An ISRC (International Standard Recording Code) identifies a single recording, and Jellyfin does not store it.
  When MusicBrainz returns several ISRCs for one recording, the plugin takes the first one.

A few other features depend on this integration:

- [Immediate favorite sync](#immediate-favorite-sync) — recording MBID
- Favorite sync from Jellyfin to ListenBrainz — recording MBID
- Favorite sync from ListenBrainz to Jellyfin — recording MBID
- [Generated playlist sync](#enable-generated-playlist-sync) — to find related recordings while matching tracks

## Backup

##### Backup path

The directory that holds the listen backups. Leave it empty and no backups are created at all, no matter how the
users are configured. You also have to select [Enable listens backup](#enable-listens-backup) per user.

Each ListenBrainz user gets a directory and each day gets a file:
`<backup path>/<listenbrainz username>/yyyy-MM-dd.json`. The format is identical the listen export of ListenBrainz.

## Libraries

##### Allowed libraries for listen submission

The libraries the plugin submits listens from; tracks in any other library are ignored. A track that is in
several libraries is submitted as long as at least one of them is allowed.

By default all music libraries are allowed.
