# Upgrading from version 2.x and earlier

For version 3.x, the plugin has been completely rewritten and the plugin configuration from earlier versions is not
compatible. To make this transition as effortless as possible, the plugin provides a migration task, which takes the old
configuration file and transforms it to the new one. This task should automatically run on server start after installing
the v3 version.

When the migration is done, a file in the plugin directory, named `.migrated` (hidden file) is created. If you wish to
run the migration again for some reason, delete this file and either restart the server or go to the scheduled tasks and
run the migration task manually.

If everything went well, you don't have to do anything. The plugin should work as usual, as if there was no migration in
the first place.

In case of a failure, the plugin will simply start with empty config. A nice side effect of the plugin (assembly) name
change is that the old plugin configuration is automatically left as is (backup). You can either choose to investigate
what went wrong or do the migration manually. Of course, you can also just configure the plugin again.

Additionally, if the migration fails and you only notice after some time, your listens should be saved in the cache (new
feature in v3 version). In that case, make sure the plugin is configured and then you can either let the plugin attempt
to resubmit the listens automatically, or you can trigger the relevant task manually.

### Changes

If you want to migrate the config yourself or you just want to manually verify the configuration migration, here is a
list of changes:

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
