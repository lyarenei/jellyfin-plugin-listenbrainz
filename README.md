# ListenBrainz plugin for Jellyfin

![ListenBrainz logo for Jellyfin plugin](res/listenbrainz/ListenBrainz_logo.svg "ListenBrainz logo for Jellyfin plugin")

This plugin is a reimplementation of the [LastFM plugin](https://github.com/jesseward/jellyfin-plugin-lastfm) for ListenBrainz.

> ListenBrainz keeps tracks of what music you listen to and provides you with insights into your listening habits.
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
  - Only one way: Jellyfin -> Listenbrainz
  - Only when pushing listen, due to technical limitations

# Installation

The plugin can be installed either via repository or [manually](#manual-build-and-installation).

## Install via repository

- Repo name: Listenbrainz (or whatever, can be anything)
- Repo URL: `https://raw.githubusercontent.com/lyarenei/jellyfin-plugin-listenbrainz/master/manifest.json`

After you add the repository, you should be able to see a Listenbrainz plugin in the catalog.
Install plugin version according to the table below and restart the server as asked.
Continue with plugin [configuration](#configuration).

Version compatibility table:

| Plugin version | Jellyfin version |
|----------------|------------------|
| 1.x.y.z        | 10.7.x           |
| 2.x.y.z        | 10.8.x           |

## Configuration

After the plugin installation, you need to configure it for your users.
Unfortunately, the server admin must configure the plugin for users,
since there's no way to make user-configurable plugin (or at least I'm not aware of it).
That means, the admin inevitably has an access to the Listenbrainz API keys of all users on their server.

To configure a user:

1. Open plugin settings
2. Select the user you want to configure
3. Paste the Listenbrainz user token to the API token field (you can get it from [here](https://listenbrainz.org/profile/))
4. Click on Get name (this will verify your API token)
5. Check `Enable listen submitting`, optionally favorites sync
6. Click on save

# Manual build and installation

.NET core 6.0 is required to build the Listenbrainz plugin.
To install the .NET SDK, check out the download page at https://dotnet.microsoft.com/download.
Native package manager instructions can be found for Debian, RHEL, Ubuntu, Fedora, SLES, and CentOS.

Once the SDK is installed, run the following.

```
git clone https://github.com/lyarenei/jellyfin-plugin-listenbrainz
cd jellyfin-plugin-listenbrainz
dotnet publish -c Release
```

If the build is successful, the compiler will report the path to your Plugin dll (`Jellyfin.Plugin.Listenbrainz/bin/Release/net6.0/Jellyfin.Plugin.Listenbrainz.dll`)

Copy the plugin DLL file into your Jellyfin ${CONFIG_DIR}/plugins/Listenbrainz_{VERSION} directory.
Create the Listenbrainz directory if it does not exist, and make sure Jellyfin can access it.
After restarting the server the plugin should be picked up and running.

# Running Jellyfin server

Check out instructions on the [offical website](https://jellyfin.org/downloads/).

# License

This plugin is directly based on an implementation of LastFM plugin, which was adapted to Jellyfin by [Jesse Ward](https://github.com/jesseward).
As they explain in the plugin README, the original Emby plugin didn't have a (compatible) license and so this plugin cannot (probably) have one either.
Due to a missing license, this plugin cannot be distributed with Jellyfin.

As far as my code is concerned, MIT license applies.
