# ListenBrainz plugin for Jellyfin

![ListenBrainz logo for Jellyfin plugin](res/listenbrainz/ListenBrainz_logo.svg "ListenBrainz logo for Jellyfin plugin")

This plugin is a reimplementation of the [LastFM plugin](https://github.com/jesseward/jellyfin-plugin-lastfm) for ListenBrainz.

> ListenBrainz keeps track of what music you listen to and provides you with insights into your listening habits.
You can use ListenBrainz to track your listening habits, discover new music with personalized recommendations,
and share your musical taste with others using our visualizations.

&mdash; <cite>[ListenBrainz][1]</cite>

[1]: https://listenbrainz.org

## Features

- Track your listening history as you play the music
- Push 'Now Playing' updates when you play something new
- Support for submitting optional MusicBrainz metadata (requires relevant MusicBrainz tags in Jellyfin)
  - Recording ID is fetched externally (if MusicBrainz integration is enabled)
- Mark favorite songs in Jellyfin as loved in ListenBrainz
  - Only one way: Jellyfin -> ListenBrainz
  - Only when pushing listen
  - Reverse sync is not possible due to how ListenBrainz works

# Installation

The plugin can be installed either via repository or [manually](#manual-build-and-installation).
All plugin releases are available on the [releases page](https://github.com/lyarenei/jellyfin-plugin-listenbrainz/releases).

## Install via repository

The plugin is available on: `https://repo.xkrivo.net/jellyfin/manifest.json`

Head over to Repositories tab in Jellyfin server settings > Plugins (advanced section), and add the repository there, using the URL above.
Repository name does not matter, it can be anything and has only informational purpose for the server admin.

After you add the repository, you should be able to see `ListenBrainz` plugin in the catalog under `General` category.
Install version according to the compatibility table below and restart the server as asked.
Continue with plugin [configuration](#configuration).

Version compatibility table:

| Plugin  | Jellyfin |
|---------|----------|
| 1.x.y.z | 10.7.a   |
| 2.x.y.z | 10.8.a   |

## Configuration

The plugin configuration has two parts, global and user. Each section is described in more detail below.

### Global configuration

**Please note that in order to apply changes in this section, you need to restart the plugin (server).**

#### Alternative API URLs
You can set alternative URLs for ListenBrainz and MusicBrainz instances which are compatible with their APIs.
By default, these URLs are set to official MetaBrainz instances.

#### MusicBrainz integration
Jellyfin does not store all metadata which could be used by ListenBrainz.
So for some data, the plugin reaches out to MusicBrainz.
Turning this integration off does not have an effect on the plugin functionality,
but the plugin will naturally be limited in what can be sent to ListenBrainz.
Most users will leave this on.

##### Data pulled from MusicBrainz
This section describes what data are currently pulled from MusicBrainz and why.

###### Recording MBID
ListenBrainz only links to MusicBrainz with recording ID, but Jellyfin only stores track ID.
With this integration off, the plugin simply cannot provide the ID, so ListenBrainz cannot link your listen to an entry in MusicBrainz.
ListenBrainz may try to infer the recording ID from the other provided data though.

###### Full artist credit
Sometimes, multiple artists are credited for a track.
While track metadata allows storing multiple artists, the format is unfortunately not standardized.
Because of that, Jellyfin may recognize a single artist as two separate artists.
Having to deal with multiple artists is one problem, but another problem is how to properly credit all of them for the track.
Usually, metadata managers will write such full artist credit string in a separate field,
but this is not standardized and not recognized by Jellyfin.

To work around this, the plugin gets the full artist credit and correct joinphrases from MusicBrainz.
With this integration off, the plugin will send only the first artist (usually the album artist).

#### Alternative listen recognition
The plugin has two distinct approaches for recognizing listens. The `PlaybackStopped` approach and `UserData` approach.
Both approaches have their own advantages and disadvantages, so the user needs to decide what is better for them.
By default, the plugin uses the `PlaybackStopped` approach.

##### PlaybackStopped approach
This approach was inherited from the LastFM plugin and uses `PlaybackStopped` event for recognizing listens.
This has the advantage of being able to check if a playback is valid from ListenBrainz point of view (4 minutes or 50% of playback).
A major disadvantage is that this approach is only suitable for online-only applications.

If clients are playing tracks offline and then want to report back to server once they come online, it makes no sense for them to send `PlaybackStopped` calls.
Additionally, a client may not even send this call (or send it with 0 time played - which will be ignored by the plugin).

##### UserData approach
This approach makes use of process of marking items as played, which emits `UserDataSaved` event.
This has a major advantage that it is suitable for both online and offline scenarios,
because it is possible to specify at what time was the item marked as played.
That way, it is possible to record listens retroactively.

However, this approach has some disadvantages which are described below.
These disadvantages are mostly coming from placing too much trust on the client and Jellyfin not having any rules for marking items as played.
Ultimately, the client should have its own ListenBrainz integration, so it would be directly responsible for sending listens to ListenBrainz.

The disadvantages are:
- Inability to check validity - as the API endpoint is only used for marking items as played and when,
  there is no way for plugin to enforce ListenBrainz rules for listen submission, since there is no provided information about playback position.
  It is completely up to the client to properly take note if the track has been played in a meaningful way or not and mark track as played accordingly.
- Client can send only the last time when track was played - this is an issue for offline playback, as the client can simply send only the last time the track has been played,
  while disregarding any playback of the same track happening in the past. Again, it is up to the client to properly report all meaningful playbacks of all tracks while offline.
- Optional `datePlayed` field - this is also an issue for offline playback - if the client does not bother with filling out this field, all listens will default to current time.

To partially work around the first limitation, the plugin internally notes what tracks are being played.
However, this is only possible for online playback - as the plugin relies on the `PlaybackStarted` event for this to work.
If the server is notified, then the plugin will save the time and when `UserDataSave` event is emitted for the same track and user,
it will compare the current time with the playback start time.
If the delta between these two times satisfies ListenBrainz submission rules, the listen for that track will be sent.
This allows to filter out too short playbacks on clients which are too eager to mark item as played (i.e.: Jellyfin Web).

In case of offline playback, there will be no `PlaybackStart` event emitted, so if a `UserDataSave` event is emitted without matching `PlaybackStart` event,
the plugin will assume so and the listen will be sent with the date specified in the event (or current time if not provided).

### User configuration

To actually use the plugin functions, you obviously need to configure it first.
Unfortunately, the server admin must configure the plugin for all users
as there's no way to make user-configurable plugin (or at least I'm not aware of it).
That means, the admin inevitably has an access to the ListenBrainz API keys of all users on their server.

To configure a user:

1. Open plugin settings
2. Select the user you want to configure
3. Paste the ListenBrainz user token to the API token field (you can get it from [here](https://listenbrainz.org/profile/))
4. (Optional) Click on `Get name` to verify the API token
5. Check `Enable listen submitting`, optionally favorites sync
6. Save configuration

# Development

This should be somewhat similar to standard .NET project development.
So, clone the repo, open it in your favorite editor, restore dependencies and you should be good to go.
Debugging setup is documented in the [jellyfin plugin template](https://github.com/jellyfin/jellyfin-plugin-template#6-set-up-debugging).

## Manual build and installation

.NET 6.0 is required to build the plugin.
To install the .NET SDK, check out the [.NET download page](https://dotnet.microsoft.com/download).

Once the SDK is installed, you should be able to compile the plugin in either debug or release configuration:

```shell
> dotnet publish -c Debug
```

```shell
> dotnet publish -c Release
```

Once the build is completed, the compiled DLL should be available at:
- For `Debug` configuration: `Jellyfin.Plugin.Listenbrainz/bin/Debug/net6.0/Jellyfin.Plugin.Listenbrainz.dll`
- For `Release` configuration: `Jellyfin.Plugin.Listenbrainz/bin/Release/net6.0/Jellyfin.Plugin.Listenbrainz.dll`

Copy the plugin DLL file into your Jellyfin `${CONFIG_DIR}/plugins/Listenbrainz` directory.
Create the Listenbrainz directory if it does not exist, and make sure Jellyfin has correct permissions to access it.
After restarting Jellyfin, the plugin should be picked up and active.

## Making a plugin release

1. Make sure you have written changes to release file in [.github](.github) directory (should be part of feature pull request)
2. Make sure you have correctly updated (according to semver rules) plugin version (should be part of feature pull request)
3. Push tag with new version
4. Update [manifest.json](manifest.json) with new release

# Jellyfin?

This repository only contains the source code for the ListenBrainz plugin for Jellyfin Media Server.
If you somehow arrived here without knowing what Jellyfin is, check out the [offical website](https://jellyfin.org).

# License

This plugin is directly based on an implementation of LastFM plugin, which was adapted to Jellyfin by [Jesse Ward](https://github.com/jesseward).
As they say in the plugin README, the original Emby plugin didn't have a (compatible) license and so this plugin cannot (probably) have one either.
Due to a missing license, this plugin cannot be distributed with Jellyfin.

As far as my code is concerned, MIT license applies.
