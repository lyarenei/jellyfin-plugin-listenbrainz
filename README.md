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

| Plugin  | Jellyfin | Status      |
|---------|----------|-------------|
| 1.x.y.z | 10.7.a   | Unsupported |
| 2.x.y.z | 10.8.a   | Unsupported |
| 3.x.y.z | 10.8.a   | Supported   |

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

### Debug logging

Please always make sure to provide debug logs when reporting a plugin issue.
To set up debug logging, you need to modify the logging configuration of the Jellyfin server.
In addition to changing the logging level, it is also necessary to update the log template, to properly display logged data.

To set up debug logging:

First, follow the steps described [here](https://jellyfin.org/docs/general/administration/troubleshooting/#debug-logging)
to enable debug logging. Then, in the same file, you will see two log templates (`outputTemplate`). One template is for
a console output, the other one is for a file output. If you are using the Jellyfin UI to collect the logs, then modify
the **File** template by adding `{EventId}`, `{ClientRequestId}` and `{HttpRequestId}` fields.
- `EventId` identifies the event which is being processed (playback start/stop, user data save)
- `ClientRequestId` identifies a ListenBrainz API request being processed
- `HttpRequestId` identifies a specific HTTP request being processed

The modified template should look like this:

```diff
- "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ThreadId}] {SourceContext}: {Message}{NewLine}{Exception}"
+ "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{ThreadId}] {SourceContext} {EventId} {ClientRequestId} {HttpRequestId}: {Message}{NewLine}{Exception}"
```
After modifying the template, restart Jellyfin server and you should see extra whitespace (before the `:`) in the log
like this: <code>... &#91;INF] &#91;1] Main&nbsp;&nbsp;&nbsp;: Jellyfin version: "10.8.13"</code>
This is expected, as the three new fields you added earlier are only defined in this plugin, while the template affects
all log messages. Unfortunately, Jellyfin does not seem to be using a logging extension which allows dynamic log templates.

Do not forget to revert these changes after you are done with capturing the logs. You can keep the template if you don't
mind the additional IDs, but make sure you change the log level back to `Information` as debug logging can in general
have an impact on the application performance.

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

To install the plugin for the first time, copy the following **DLL** files to the plugin directory in your
Jellyfin installation (`${CONFIG_DIR}/plugins/Jellyfin.Plugin.ListenBrainz`). Create the `Jellyfin.Plugin.ListenBrainz`
directory if it does not exist, and make sure Jellyfin has correct permissions to access it.

- All files starting with `Jellyfin.Plugin.ListenBrainz`
- All files starting with `MetaBrainz`

After restarting Jellyfin, the plugin should be recognized and activated. If you forgot any of these files, then the
plugin will crash during initialization and in the log, you should see which DLL is missing.

It is not necessary to copy all the files every time. For subsequent builds of the plugin it is enough to copy only the
recompiled files.

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
