namespace Jellyfin.Plugin.ListenBrainz.Configuration;

/// <summary>
/// Migration of the deprecated playlist sync settings to the new playlist sync settings.
/// </summary>
internal static class LegacyPlaylistSyncMigration
{
    internal static bool Apply(PluginConfiguration config)
    {
        var syncAllPlaylists = config.IsAllPlaylistsSyncEnabled;
        var userConfigs = config.UserConfigs.Where(uc => uc.IsPlaylistsSyncEnabled).ToList();
        if (userConfigs.Count == 0 && !syncAllPlaylists)
        {
            return false;
        }

        foreach (var userConfig in userConfigs)
        {
            // Old, deprecated setting => set to false
            userConfig.IsPlaylistsSyncEnabled = false;

            // If already using new settings => ignore
            if (userConfig.IsGeneratedPlaylistsSyncEnabled)
            {
                continue;
            }

            // Enable new playlist sync settings for user
            userConfig.IsGeneratedPlaylistsSyncEnabled = true;
            if (syncAllPlaylists)
            {
                userConfig.IsWeeklyJamsSyncEnabled = true;
                userConfig.IsWeeklyExplorationSyncEnabled = true;
                userConfig.IsTopDiscoveriesSyncEnabled = true;
                userConfig.IsTopMissedRecordingsSyncEnabled = true;
            }
        }

        // Disable deprecated setting
        config.IsAllPlaylistsSyncEnabled = false;
        return true;
    }
}
