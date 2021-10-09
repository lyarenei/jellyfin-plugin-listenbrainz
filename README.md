# jellyfin-plugin-listenbrainz

This plugin is a reimplementation of the [LastFM plugin](https://github.com/jesseward/jellyfin-plugin-lastfm) for Listenbrainz. I removed all functionality which does not apply to Listenbrainz and appropriately renamed files/variables/methods.

This repository was originally a fork of the LastFM plugin, but I chose to detach it, since this plugin will go in a different (incompatible) way - naturally, as it's for a different service.

## Features
- Track your listening history as you play the music
- Push 'Now Playing' updates when you play something new
- Support for optional MusicBrainz metadata
  - This obviously requires relevant tags in the files
  - Recording ID is fetched externally (from v1.1.x)

## About Listenbrainz

ListenBrainz keeps tracks of what music you listen to and provides you with insights into your listening habits. Listenbrainz is completely open-source and publish their data as open data.

You can use ListenBrainz to track your music listening habits and share your taste with others using generated visualizations.
ListenBrainz is operated by the MetaBrainz Foundation which has a long-standing history of curating, protecting and making music data available to the public.

(from [listenbrainz.org](https://listenbrainz.org) (modified))


# Repo Install

Jellyfin 10.6.0 introduces 3rd party plugin repositories (see: [announcement](https://jellyfin.org/posts/plugin-updates/)), configure the following to follow stable builds for this plugin
* Repo name: Listenbrainz
* Repo URL: `https://raw.githubusercontent.com/lyarenei/jellyfin-plugin-listenbrainz/master/manifest.json`


# Build

.NET core 5.0 is required to build the Listenbrainz plugin. To install the .NET SDK on Linux or macOS, see the download page at https://dotnet.microsoft.com/download. Native package manager instructions can be found for Debian, RHEL, Ubuntu, Fedora, SLES, and CentOS.

Once the SDK is installed, run the following.

```
git clone https://github.com/lyarenei/jellyfin-plugin-listenbrainz
cd jellyfin-plugin-listenbrainz
dotnet build -c release
```

If the build is successful, the compiler will report the path to your Plugin dll (`Jellyfin.Plugin.Listenbrainz/bin/Release/net5.0/Jellyfin.Plugin.Listenbrainz.dll`)

## Manual Installation

Copy the plugin DLL file into your Jellyfin ${CONFIG_DIR}/plugins/Listenbrainz_{VERSION} directory.
Create the Listenbrainz directory if it does not exist, and make sure Jellyfin can access it.

# Running Jellyfin server

See instructions on the [offical website](https://jellyfin.org/downloads/).

# License
This plugin is directly based on an implementation of LastFM plugin, which was adapted to Jellyfin by [Jesse Ward](https://github.com/jesseward).
As they explain in the plugin README, the original Emby plugin didn't have a (compatible) license and so this plugin cannot have one either.
Due to a missing license, this plugin cannot be distributed with Jellyfin.
