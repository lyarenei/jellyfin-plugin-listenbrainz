using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Configuration.Migrations;

/// <summary>
/// Reads plugin config file and applies all applicable migrations.
/// </summary>
internal sealed class PluginConfigMigrator
{
    internal const string BackupSuffix = ".bak";
    internal const string CorruptedSuffix = ".corrupted.bak";

    private static readonly IConfigMigration[] _migrations = [new LegacyPlaylistSyncMigration()];

    private readonly string _configFilePath;
    private readonly string _backupFilePath;
    private readonly string _corruptedFilePath;
    private readonly IXmlSerializer _xmlSerializer;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfigMigrator"/> class.
    /// </summary>
    /// <param name="configFilePath">Path to the plugin configuration file.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    /// <param name="logger">Logger instance.</param>
    public PluginConfigMigrator(string configFilePath, IXmlSerializer xmlSerializer, ILogger logger)
    {
        _configFilePath = configFilePath;
        _backupFilePath = configFilePath + BackupSuffix;
        _corruptedFilePath = configFilePath + CorruptedSuffix;
        _xmlSerializer = xmlSerializer;
        _logger = logger;
    }

    internal static int LatestVersion => _migrations.Max(m => m.TargetVersion);

    /// <summary>
    /// Loads the plugin configuration and runs migration up to <see cref="LatestVersion"/>.
    /// </summary>
    /// <param name="saveConfig">Callback for saving the configuration to a file.</param>
    /// <returns>Current configuration.</returns>
    public PluginConfiguration LoadAndMigrate(Action<PluginConfiguration> saveConfig)
    {
        if (!File.Exists(_configFilePath))
        {
            _logger.LogInformation("No plugin configuration file found, creating a new one");
            var newConfig = new PluginConfiguration { ConfigVersion = LatestVersion };
            saveConfig(newConfig);
            return newConfig;
        }

        var config = LoadConfig(_configFilePath);
        if (config is null)
        {
            PreserveCorruptedConfig();
            return new PluginConfiguration();
        }

        if (config.ConfigVersion > LatestVersion)
        {
            _logger.LogInformation(
                "Skipping migration - plugin configuration is on a newer version than current latest version {LatestVersion}",
                LatestVersion);

            return config;
        }

        if (config.ConfigVersion == LatestVersion)
        {
            return config;
        }

        var success = TryCreateBackup();
        if (!success)
        {
            _logger.LogWarning("Skipping migration - the configuration file could not be backed up");
            return config;
        }

        try
        {
            ApplyPendingMigrations(config);
            saveConfig(config);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Migration failed, attempting to restore the configuration from the backup");
            return RestoreFromBackup();
        }

        RemoveBackup();
        return config;
    }

    private void ApplyPendingMigrations(PluginConfiguration config)
    {
        var pendingMigrations = _migrations
            .Where(m => m.TargetVersion > config.ConfigVersion)
            .OrderBy(m => m.TargetVersion);

        foreach (var migration in pendingMigrations)
        {
            _logger.LogInformation(
                "Migrating plugin configuration to version {TargetVersion} ({MigrationName})",
                migration.TargetVersion,
                migration.Name);

            var success = migration.Apply(config);
            if (!success)
            {
                _logger.LogWarning(
                    "Migration to version {TargetVersion} ({MigrationName}) failed, stopping",
                    migration.TargetVersion,
                    migration.Name);
                return;
            }

            config.ConfigVersion = migration.TargetVersion;
        }
    }

    private PluginConfiguration? LoadConfig(string filePath)
    {
        try
        {
            return (PluginConfiguration)_xmlSerializer.DeserializeFromFile(typeof(PluginConfiguration), filePath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to read plugin configuration file {FilePath}", filePath);
            return null;
        }
    }

    private bool TryCreateBackup()
    {
        try
        {
            File.Copy(_configFilePath, _backupFilePath, true);
            _logger.LogInformation("Backed up plugin configuration to {BackupFilePath}", _backupFilePath);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to back up plugin configuration to {BackupFilePath}", _backupFilePath);
            return false;
        }
    }

    private PluginConfiguration RestoreFromBackup()
    {
        try
        {
            File.Copy(_backupFilePath, _configFilePath, true);
            _logger.LogInformation("Plugin configuration has been restored from {BackupFilePath}", _backupFilePath);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to restore plugin configuration from {BackupFilePath}", _backupFilePath);
        }

        // Do not delete backup file as it may be needed if restore was necessary
        _logger.LogWarning("Plugin configuration backup is kept at {BackupFilePath}", _backupFilePath);

        // Use defaults if load from backup fails; next restore can be attempted on server restart
        return LoadConfig(_backupFilePath) ?? new PluginConfiguration();
    }

    private void RemoveBackup()
    {
        try
        {
            File.Delete(_backupFilePath);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to delete plugin configuration backup file at {BackupFilePath}", _backupFilePath);
        }
    }

    private void PreserveCorruptedConfig()
    {
        try
        {
            File.Copy(_configFilePath, _corruptedFilePath, true);
            _logger.LogWarning(
                "Plugin configuration file is corrupted and plugin will start with a default configuration" +
                " - a copy of the config file has been preserved at {CorruptedFilePath}",
                _corruptedFilePath);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Could not create a copy corrupted plugin configuration file at {CorruptedFilePath}",
                _corruptedFilePath);
        }
    }
}
