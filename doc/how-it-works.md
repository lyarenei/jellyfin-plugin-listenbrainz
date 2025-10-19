# How does the plugin work

Here is a general description of how particular plugin features work. The plugin configuration is documented separately
and can be found [here](configuration.md).

## Sending listens

Sending listens is the main function of this plugin. In general, there are two types of a listen recognized by
ListenBrainz (technically three, but the third one is just an extension). Each type is evaluated differently and the
process is described below.

### Sending 'now playing' listen

Sending `now playing` listen does not have any criteria and is completely optional. From the plugin perspective, the
process begins by picking up a `PlaybackStart` event emitted by the server when informed of a playback start. After
verifying that all required data are available for sending a listen, the plugin also checks if the user is configured
and has enabled listen submission. Naturally, if everything is good, then the plugin fetches additional metadata from
MusicBrainz (if enabled) and `now playing` listen is sent. As this listen type is not important that much, there is no
error handling, besides some basic retrying handled automatically by the API client.

### Sending general listen

The process for sending a listen begins pretty much the same as for `now playing` listen. There are 2 important
differences though. The first one is that there is an additional requirement - the playback time of a track must be
either at least 4 minutes or a half of its runtime. The second one is related to the event triggering this process.
Depending on the configuration, the plugin will either react on a `PlaybackStopped` or `UserDataSaved` event emitted by
the server. The first one is emitted when the server is informed about a playback stop. The second one is emitted when
any kind of user data for that particular track is being saved. The plugin specifically watches for events with a reason
for `PlaybackFinished`. These two modes are documented in more
detail [here](configuration.md#use-alternative-event for-recognizing-listens). After checking all other conditions, the
plugin will send a listen for the specified track. In case of a failure, the listen is automatically saved into a listen
cache to retry later.

## Listen cache

In case of listen submit failures, the listens are saved into a cache, so the data are not lost and the plugin can retry
sending them in the future. The retry window is randomized on every server startup, with the window being no less than
24 hours and no more than 25 hours. If you wish to try resubmitting the listens right away, you can do so by triggering
the scheduled task in the server admin interface. Favorites are not synced during this process.

If a user does not have a valid configuration or has listen submitting disabled, no listens will be recorded in the
cache for that user.

## Syncing favorites

In addition to listen submission, this plugin also offers favorite sync. Or, more exactly, marking favorite tracks in
Jellyfin as `loved` in ListenBrainz (and vice-versa). Synchronizing favorite artists and albums are not supported as
this is not supported by ListenBrainz. Similarly, `hated` listens in ListenBrainz are not synced to Jellyfin as there
is no such concept in Jellyfin.

#### From Jellyfin to ListenBrainz

Syncing always takes place right away after successfully submitting a listen. Please note it may take some time for the
hearts to be updated in the ListenBrainz UI. Primarily, a recording MBID is used for the sync process, but if it's not
available, the process falls back to using MSID.

In the MSID case, you may see additional requests made for API token verification. This is to get a ListenBrainz
username associated with the API token (the plugin did not store the username in earlier 3.x versions). If you wish to
avoid this, go to plugin settings and save the user configuration, no changes are necessary. Upon saving, the plugin
will try getting the username and save it in the configuration.

When using MSID for the sync, the plugin tries to find the correct MSID at exponential intervals, up to 4 attempts
(around 10 minutes). If the MSID is still not found, then the sync is cancelled.

#### From ListenBrainz to Jellyfin

Currently, only a manual task is available at this moment. This is because of an absence of recording MBIDs which make
matching MBIDs to tracks a very expensive operation (in terms of time) and so it is impractical to run this sync
regularly.

You can run the sync task from the Jellyfin administration menu (under scheduled tasks). The task pulls loved listens
for all users which have favorite synchronization enabled. Keep in mind, that the task can take a long time to complete.
Hopefully this will change at some point in the future.

For reference, a library of approximately 4000 tracks takes around 70 minutes to complete. This is then multiplied by
number of users which have favorite syncing enabled (assuming all users have access to all tracks on the server).

## Syncing playlists

Every week, on Monday, ListenBrainz automatically generates several playlists for all users. If the user has enabled
playlists sync in settings, the plugin automatically recreates these playlists in Jellyfin. By default, only playlists "
from the past" are recreated (weekly jams and top discoveries). Syncing of all playlists can be enabled in the plugin
settings. Empty playlists are always ignored.

This feature **requires** a `recording MBID` in the song/audio metadata for sucessfully identifying and assinging a
song to a playlist. There is no fallback to `MusicBrainz` API like other features have.

A sync is triggered automatically every Monday. However, the time of the day is randomized on every server start, to
spread out the load on ListenBrainz servers. If necessary, the sync task can be also run manually at any time from the
Jellyfin administration UI.

If there is an already existing playlist with the same name, it will be automatically deleted and recreated. The
playlists created by the plugin will always have a `[LB]` prefix followed by the playlist name. If, for some reason, you
want to preserve a playlist, simply rename it in Jellyfin and the playlist will be ignored by the plugin.
