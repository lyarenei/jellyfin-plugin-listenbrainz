# jellyfin-plugin-listenbrainz

This plugin is a reimplementation of the [LastFM plugin](https://github.com/jesseward/jellyfin-plugin-lastfm) (the scrobbling function) for Listenbrainz. I removed all functionality which does not apply to Listenbrainz and appropriately renamed files/variables/methods.

As the LastFM plugin was migrated by [Jesse Ward](https://github.com/jesseward) from the original Emby repository, this plugin *cannot* be distributed with Jellyfin due to a missing compatible license.

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

.NET core 5.0 is required to build the Listenbrainz plugin. To install the .NET SDK on Linux or macOS, see the download page at https://dotnet.microsoft.com/download . Native package manager instructions can be found for Debian, RHEL, Ubuntu, Fedora, SLES, and CentOS.

Once the SDK is installed, run the following.

```
git clone https://github.com/lyarenei/jellyfin-plugin-listenbrainz
cd jellyfin-plugin-listenbrainz
dotnet build
```

# Manual Install

If the build is successful, the tool will report the path to your Plugin dll (`Jellyfin.Plugin.Listenbrainz/bin/Debug/net5.0/Jellyfin.Plugin.Listenbrainz.dll`)

The plugin should then be copied into your Jellyfin ${CONFIG_DIR}/plugins directory.

# Running Jellyfin server

See instructions on the [offical website](https://jellyfin.org/downloads/).
