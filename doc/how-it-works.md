# How does the plugin work

Here is a general description of how particular plugin features work. The plugin configuration is documented separately
and can be found [here](configuration.md). If you are upgrading from versions 2 and below, it might be worth to check
out the [migration documentation](migration.md) as well.

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
the scheduled task in the server admin interface. When resubmitting listens, the favorites are not synced.

If a user does not have a valid configuration or has listen submitting disabled, no listens will be recorded in the
cache for that user.

## Syncing favorites

In addition to listen submission, this plugin also offers favorite sync. Or, more exactly, marking favorite tracks in
Jellyfin as `loved` in ListenBrainz. The syncing takes place right away after successfully submitting a listen.

For now, the process in only one-way, from Jellyfin to ListenBrainz. The other direction is not currently implemented,
but it is planned.
