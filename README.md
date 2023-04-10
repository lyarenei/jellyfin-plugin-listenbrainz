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
- Support for submitting optional MusicBrainz metadata
  - Requires relevant MusicBrainz tags in Jellyfin
  - Recording ID is fetched externally (from v1.1.x)
- Mark Jellyfin favorite songs as loved (from v1.2.x)
  - Only one way: Jellyfin -> ListenBrainz
  - Only when pushing listen
  - Reverse sync is not possible due to how ListenBrainz works (at least for now)

# Installation

The plugin can be installed either via repository or [manually](#manual-build-and-installation).

## Install via repository

The repository only serves two last versions of the plugin per Jellyfin version.
If you need an older version for some reason, you'll need to install it manually.
The releases are available on the [releases page](https://github.com/lyarenei/jellyfin-plugin-listenbrainz/releases).

### Reposiotory setup

- Repo name: ListenBrainz (or whatever, can be anything)
- Repo URL: `https://raw.githubusercontent.com/lyarenei/jellyfin-plugin-listenbrainz/master/manifest.json`

After you add the repository, you should be able to see the plugin in the catalog under `General` category.
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
Jellyfin does not store all metadata which could be used by LisnteBrainz.
So for some data, the plugin reaches out to MusicBrainz.
Turning this integration off does not have an effect on the plugin functionality,
but in that case, the plugin will naturally be limited in what can be sent to ListenBrainz.
Most users will leave this on, but if you wish this integration off, you can do so.

##### Data pulled from MusicBrainz
This section describes what data are currently pulled from MusicBrainz and why.

###### Recording MBID
ListenBrainz only links to MusicBrainz with recording ID, but Jellyfin only stores track ID.
These are not the same values unfortunately. With this integration off, the plugin simply cannot provide the ID,
so ListenBrainz cannot link your listen to an entry in MusicBrainz.
However, ListenBrainz may try to infer the recording ID from the other provided data.

###### Full artist credit
Sometimes, multiple artists collaborate on a track/song.
While there can be multiple artists in metadata, the format is not standardized,
so in some cases, Jellyfin may recognize a single artist as two separate artists.
Having to deal with multiple artists is one problem, but another problem is how to properly credit all of them for the track/song.
Usually, metadata managers will write such full artist credit string in a separate field,
but then again this is not standardized and not recognized by Jellyfin either.

To avoid these issues, the plugin gets the full artist credit and correct joinphrases from MusicBrainz.
With this integration off, the plugin will simply send only the first artist (usually the album artist).

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

Note:
I'm not that much experienced with C# development, so my approach might not make much sense.
But this is what I was ultimately able to come with and it works.
If you know a better way, please let me know.

---

The repository currently contains two projects (excluding something what might be considered as tests).
The first one is for Jellyfin 10.7 - `Jellyfin.Plugin.Listenbrainz`. The other one is for Jellyfin 10.8.
The 10.8 one uses file links, so the build process uses single code source for both versions.
As long as the build command/job/whatever passes, you'll be fine.

If you are adding a new file or deleting one or you are somehow changing the file structure,
don't forget to reflect the changes in the 10.8 version project as well.
If you forget, the compiler will complain about missing files, unresolved references, etc...

## Manual build and installation

.NET 6.0 is required to build the plugin.
To install the .NET SDK, check out the download page at https://dotnet.microsoft.com/download.
Native package manager instructions can be found for Debian, RHEL, Ubuntu, Fedora, SLES, and CentOS.

Once the SDK is installed, run the following:

```shell
> git clone https://github.com/lyarenei/jellyfin-plugin-listenbrainz
> cd jellyfin-plugin-listenbrainz
> dotnet publish -c Release
```

This will build both versions, for Jellyfin 10.7 and 10.8.
The built DLL locations should be as follows:
- For JF 10.7 plugin binary: `Jellyfin.Plugin.Listenbrainz/bin/Release/net5.0/Jellyfin.Plugin.Listenbrainz.dll`
- For JF 10.8 plugin binary: `Jellyfin.Plugin.Listenbrainz.JF108/bin/Release/net6.0/Jellyfin.Plugin.Listenbrainz.dll`

Copy the plugin DLL file into your Jellyfin `${CONFIG_DIR}/plugins/Listenbrainz` directory.
Create the Listenbrainz directory if it does not exist, and make sure Jellyfin can access it.
After restarting the server, the plugin should be picked up by the server and should be running.

## Making a release

1. Make sure you have written changes to release file in [.github](.github) directory.
2. Push tag with new version
3. Update [manifest.json](manifest.json) with new releases

# Jellyfin?

This repository only contains the source code for the ListenBrainz plugin for Jellyfin media server.
If you somehow arrived here without knowing what Jellyfin is, check out the [offical website](https://jellyfin.org).

# License

This plugin is directly based on an implementation of LastFM plugin, which was adapted to Jellyfin by [Jesse Ward](https://github.com/jesseward).
As they say in the plugin README, the original Emby plugin didn't have a (compatible) license and so this plugin cannot (probably) have one either.
Due to a missing license, this plugin cannot be distributed with Jellyfin.

As far as my code is concerned, MIT license applies.
