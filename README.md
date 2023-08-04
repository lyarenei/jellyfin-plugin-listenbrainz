<!--suppress CheckEmptyScriptTag, HtmlDeprecatedAttribute -->
<div align="center">
    <h1>ListenBrainz plugin for Jellyfin</h1>
    <image src="res/listenbrainz/ListenBrainz_logo.svg" alt="Image for ListenBrainz plugin for Jellyfin" width=500 height="300" align=center />
<p>This plugin allows you to record your music activity directly from Jellyfin server to ListenBrainz.</p>
</div>

> Visualize and share your music listening history
>
> ListenBrainz keeps track of music you listen to and provides you with insights into your listening habits.
> We're completely open-source and publish our data as open data.

&mdash; <cite>[ListenBrainz][1]</cite>

[1]: https://listenbrainz.org

## Features

- Send `listens` of tracks you've listened to
- Send 'now playing' `listens`
- Submit optional MusicBrainz metadata (requires relevant MusicBrainz track ID in Jellyfin metadata)
- Sync favorite songs in Jellyfin to ListenBrainz (marked as loved)
- Cache listens when ListenBrainz server cannot be reached

...and probably some more to come.

If you are interested in more details about how this plugin works, please check out the
[documentation](doc/how-it-works.md).

# Installation

The plugin can be installed either via repository or [manually](#manual-build-and-installation).
Additionally, all plugin releases are available on the
[releases page](https://github.com/lyarenei/jellyfin-plugin-listenbrainz/releases).

## Install via repository

The plugin is available on: `https://repo.xkrivo.net/jellyfin/manifest.json`

Head over to Repositories tab in Jellyfin server settings > Plugins (advanced section), and add the repository
there, using the URL above.
Repository name does not matter, it has only an informational purpose for the server admin.

After you add the repository, you should be able to see `ListenBrainz` plugin in the catalog under `General` category.
Install version according to the compatibility table below and restart the server as asked.
Continue with plugin [configuration](doc/configuration.md).

Version compatibility table:

| Plugin  | Jellyfin |
|---------|----------|
| 1.x.y.z | 10.7.a   |
| 2.x.y.z | 10.8.a   |
| 3.x.y.z | 10.8.a   |

## Configuration

The complete configuration documentation is available [here](doc/configuration.md).

### Quickstart

For plugin to be able to send listens, it needs user API token to be able to authenticate.
Unfortunately, the server admin must configure the plugin for all users as there's no way to make
user-configurable plugin. So the admin has an access to the ListenBrainz API tokens of all users on their
server.

Minimal user configuration:

1. Open plugin settings
2. Select the user you want to configure
3. Paste the token to the API token field (you can get it from [the profile page](https://listenbrainz.org/profile/))
4. (Optional) Click on `Verify` to check the validity of the API token
5. Check `Enable submitting listens`
6. Save configuration

# Development

This should be somewhat similar to standard .NET project development.
So, clone the repo, open it in your favorite editor, restore dependencies and you should be good to go.
Debugging setup is documented in
the [jellyfin plugin template](https://github.com/jellyfin/jellyfin-plugin-template#6-set-up-debugging).

## Manual build and installation

.NET 7.0 is required to build the plugin.
To install the .NET SDK, check out the [.NET download page](https://dotnet.microsoft.com/download).

Once the SDK is installed, you should be able to compile the plugin in either debug or release configuration:

```shell
> dotnet publish -c Debug
```

```shell
> dotnet publish -c Release
```

Once the build is completed, the compiled DLLs should be available at:
`src/Jellyfin.Plugin.Listenbrainz/bin/<Debug|Release>/netX.0/`

Unless you exactly know what you are doing, copy **all** DLL files in the directory to the plugin directory in your
Jellyfin installation (`${CONFIG_DIR}/plugins/Jellyfin.Plugin.ListenBrainz`). Create the `Jellyfin.Plugin.ListenBrainz`
directory if it does not exist, and make sure Jellyfin has correct permissions to access it. After restarting Jellyfin,
the plugin should be recognized and activated.

## Making a plugin release

1. Make sure you have updated [build file](build.yaml) (ideally should be part of feature pull request)
2. Create a new release in the repository
3. Check that the new version is available in the repository

# Jellyfin?

This repository only contains the source code for the ListenBrainz plugin for `Jellyfin Media Server`.
If you somehow arrived here without knowing what Jellyfin is, check out the [project website](https://jellyfin.org).

# License

TL;DR: MIT + GPLv3.

This plugin began its life as a reimplementation of
the [LastFM plugin](https://github.com/jesseward/jellyfin-plugin-lastfm).
While that one does not have a license, the plugin has been now completely rewritten to the point that I
believe it can be no longer considered as a derivative work. As such, I decided to license the plugin
code (except some parts as described below) under the MIT license.

However, if I understand correctly, the code which depends on Jellyfin libraries, which are licenced under GPLv3, must
be also licenced under GPLv3 license. And so this plugin is also licensed under the GPLv3 license.
