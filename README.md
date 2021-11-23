# jellyfin-plugin-listenbrainz

![ListenBrainz logo for Jellyfin plugin](res/listenbrainz/ListenBrainz_logo.svg "ListenBrainz logo for Jellyfin plugin")

This plugin is a reimplementation of the [LastFM plugin](https://github.com/jesseward/jellyfin-plugin-lastfm) for Listenbrainz. I removed all functionality which does not apply to Listenbrainz and appropriately renamed files/variables/methods.

## Features

- Track your listening history as you play the music
- Push 'Now Playing' updates when you play something new
- Support for submitting optional MusicBrainz metadata
  - This obviously requires relevant tags in the files
  - Recording ID is fetched externally (from v1.1.x)
- Sync Jellyfin favorites (from v1.2.x)
  - The sync is only one way for now - from Jellyfin to Listenbrainz
  - The sync is performed immediately after listen submission

## About Listenbrainz

ListenBrainz keeps tracks of what music you listen to and provides you with insights into your listening habits. Listenbrainz is completely open-source and publish their data as open data.

You can use ListenBrainz to track your music listening habits and share your taste with others using generated visualizations.
ListenBrainz is operated by the MetaBrainz Foundation which has a long-standing history of curating, protecting and making music data available to the public.

(from [listenbrainz.org](https://listenbrainz.org) (modified))

# Installation

The plugin can be installed either via repository or [manually](#manual-build-and-installation)

## Repo Install

Jellyfin 10.6.0 introduces 3rd party plugin repositories (see: [announcement](https://jellyfin.org/posts/plugin-updates/)), configure the following to follow stable builds for this plugin

- Repo name: Listenbrainz (or whatever, can be anything)
- Repo URL: `https://raw.githubusercontent.com/lyarenei/jellyfin-plugin-listenbrainz/master/manifest.json`

After you add the repository, you should be able to see a Listenbrainz plugin in the catalog.
Install your preferred version and restart the server as asked.

## Configuration

After the plugin installation, you need to configure it for your users.
Unfortunately, the server admin must configure the plugin for users,
since there's no way to make user-configurable plugin (or at least I'm not aware of it).
That means, the admin inevitably has an access to the Listenbrainz API keys of all users on their server.

To configure a user:

1. Open plugin settings
2. Select the user you want to configure
3. Paste the Listenbrainz user token to the API token field (you can get it from [here](https://listenbrainz.org/profile/))
4. Check `Enable listen submitting`
5. Click on save

The API token is verified before saving.
The settings will NOT be saved, if the provided token is not valid.

# Manual build and installation

.NET core 5.0 is required to build the Listenbrainz plugin. To install the .NET SDK on Linux or macOS, see the download page at https://dotnet.microsoft.com/download. Native package manager instructions can be found for Debian, RHEL, Ubuntu, Fedora, SLES, and CentOS.

Once the SDK is installed, run the following.

```
git clone https://github.com/lyarenei/jellyfin-plugin-listenbrainz
cd jellyfin-plugin-listenbrainz
dotnet publish -c Release
```

If the build is successful, the compiler will report the path to your Plugin dll (`Jellyfin.Plugin.Listenbrainz/bin/Release/net6.0/Jellyfin.Plugin.Listenbrainz.dll`)

Copy the plugin DLL file into your Jellyfin ${CONFIG_DIR}/plugins/Listenbrainz_{VERSION} directory.
Create the Listenbrainz directory if it does not exist, and make sure Jellyfin can access it.

# Running Jellyfin server

See instructions on the [offical website](https://jellyfin.org/downloads/).

# License

This plugin is directly based on an implementation of LastFM plugin, which was adapted to Jellyfin by [Jesse Ward](https://github.com/jesseward).
As they explain in the plugin README, the original Emby plugin didn't have a (compatible) license and so this plugin cannot (I think) have one either.
Due to a missing license, this plugin cannot be distributed with Jellyfin.
