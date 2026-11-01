namespace Jellyfin.Plugin.ListenBrainz.Configuration.Migrations;

/// <summary>
/// Plugin configuration migration interface.
/// </summary>
internal interface IConfigMigration
{
    /// <summary>
    /// Gets the target configuration version. Must be unique across all migrations.
    /// </summary>
    int TargetVersion { get; }

    /// <summary>
    /// Gets a name of this migration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies the migration to the specified configuration.
    /// </summary>
    /// <param name="config">Configuration to migrate.</param>
    /// <returns>Migration success.</returns>
    bool Apply(PluginConfiguration config) => false;
}
