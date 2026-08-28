namespace Jellyfin.Plugin.ListenBrainz.Configuration.Migrations;

/// <summary>
/// Migration of the deprecated playlist sync settings to the new playlist sync settings.
/// </summary>
internal sealed class LegacyPlaylistSyncMigration : IConfigMigration
{
    /// <inheritdoc />
    public int TargetVersion => 1;

    /// <inheritdoc />
    public string Name => "Legacy playlist sync settings";

    /// <inheritdoc />
    public bool Apply(PluginConfiguration config)
    {
        var syncAllPlaylists = config.IsAllPlaylistsSyncEnabled;
        var userConfigs = config.UserConfigs.Where(uc => uc.IsPlaylistsSyncEnabled).ToList();
        foreach (var userConfig in userConfigs)
        {
            // Old, deprecated setting => set to false
            userConfig.IsPlaylistsSyncEnabled = false;

            // If already using new settings => ignore
            // Special case here as the migration system was introduced after this has been released
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
