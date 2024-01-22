# Upgrading from version 2.x and earlier

For version 3.x, the plugin has been completely rewritten and the plugin configuration from earlier versions is not
compatible. To make this transition as effortless as possible, the plugin provides a task, which takes the old
configuration file and migrates it to the new one. This task should automatically run on server start after installing
the v3 version.

If the migration is successful, `.migrated` (hidden) file is created in the plugin directory. If you wish to
run the migration again for some reason, delete this file and either restart the server or go to the scheduled tasks and
run the migration task manually.

If everything went well, you don't have to do anything. The plugin should work as usual, as if there was no migration in
the first place.

In case of a failure, the plugin will simply start with empty config. A nice side effect of the plugin (assembly) name
change is that the old plugin configuration is automatically left as is (backup). You can either choose to investigate
what went wrong or do the migration manually. Of course, you can also just configure the plugin again.

### Changes

Here are all changes between plugin version 2.x (left) and 3.x (right), excluding source code changes:

#### Global configuration

- Plugin assembly name: `Jellyfin.Plugin.Listenbrainz` -> `Jellyfin.Plugin.ListenBrainz`
- ListenBrainz URL: `ListenbrainzBaseUrl` -> `ListenBrainzApiUrl`
- MusicBrainz URL: `MusicbrainzBaseUrl` -> `MusicBrainzApiUrl`
- MusicBrainz integration: `MusicbrainzEnabled` -> `IsMusicBrainzEnabled`
- Alternative mode: `AlternativeListenDetectionEnabled` -> `IsAlternativeModeEnabled`

#### User configuration

- List of user configs: `LbUsers` -> `UserConfigs`
- User config: `LbUser` -> `UserConfig`
- API token: `Token` -> `ApiToken`
- User ID: `MediaBrowserUserId` -> `JellyfinUserId`
- Enable listen submit: `ListenSubmitEnabled` -> `IsListenSubmitEnabled`
- Enable favorite sync: `SyncFavoritesEnabled` -> `IsFavoritesSyncEnabled`

Additionally, the API token is now obfuscated as a base64 string.
