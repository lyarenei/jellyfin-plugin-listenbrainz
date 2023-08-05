using System.Collections.ObjectModel;
using System.Xml.Linq;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ListenBrainz.Tasks;

/// <summary>
/// Jellyfin scheduled task to migrate old plugin configuration.
/// </summary>
public class MigrationTask : IScheduledTask
{
    private const string OldPluginConfigName = "Jellyfin.Plugin.Listenbrainz.xml";
    private const string MigratedFileName = ".migrated";

    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MigrationTask"/> class.
    /// </summary>
    /// <param name="loggerFactory">Logger factory.</param>
    public MigrationTask(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger($"{Plugin.LoggerCategory}.MigrationTask");
    }

    private static string MigratedFilePath => Path.Join(Plugin.GetDataPath(), MigratedFileName);

    /// <inheritdoc />
    public string Name => "Migrate from version 2.x and below";

    /// <inheritdoc />
    public string Key => "MigrateConfig";

    /// <inheritdoc />
    public string Description => "Migrate plugin configuration from plugin version 2.x and below to a new one.";

    /// <inheritdoc />
    public string Category => "ListenBrainz";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var configDir = Plugin.GetConfigDirPath();
        var oldPluginConfig = Path.Join(configDir, OldPluginConfigName);
        if (!File.Exists(oldPluginConfig))
        {
            _logger.LogInformation("Old plugin configuration file is not available, nothing to do");
            return;
        }

        if (File.Exists(MigratedFilePath))
        {
            _logger.LogInformation("Plugin configuration has been already migrated, nothing to do");
            return;
        }

        var configFileContent = await File.ReadAllTextAsync(oldPluginConfig, cancellationToken);
        var newConfig = MigrateConfig(configFileContent);
        if (newConfig is null)
        {
            _logger.LogWarning("Failed to migrate plugin configuration");
            return;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Plugin.UpdateConfig(newConfig);
            CreateMigratedFile();
        }
        catch (Exception)
        {
            _logger.LogInformation("Plugin config migration has been cancelled");
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerStartup,
                MaxRuntimeTicks = 10 * TimeSpan.TicksPerSecond
            }
        };
    }

    private static void CreateMigratedFile() => File.CreateText(MigratedFilePath).Close();

    private PluginConfiguration? MigrateConfig(string content)
    {
        var root = (XElement?)XDocument.Parse(content).FirstNode;
        if (root is null)
        {
            _logger.LogInformation("Could not migrate configuration, the file format is malformed");
            return null;
        }

        PluginConfiguration? newConfig = null;
        Collection<UserConfig> userConfigs = new Collection<UserConfig>();
        foreach (var element in root.Descendants())
        {
            switch (element.Name.ToString())
            {
                case "GlobalConfig":
                    newConfig = ParseGlobalConfig(element);
                    break;
                case "LbUsers":
                    userConfigs = ParseUserConfigs(element);
                    break;
            }
        }

        if (newConfig is null) return null;
        newConfig.UserConfigs = userConfigs;
        return newConfig;
    }

    private static PluginConfiguration ParseGlobalConfig(XContainer globalConfig)
    {
        var pluginConfig = new PluginConfiguration();
        foreach (var element in globalConfig.Descendants())
        {
            switch (element.Name.ToString())
            {
                case "ListenbrainzBaseUrl":
                    pluginConfig.ListenBrainzApiUrl = element.Value;
                    break;
                case "MusicbrainzBaseUrl":
                    pluginConfig.MusicBrainzApiUrl = element.Value;
                    break;
                case "MusicbrainzEnabled":
                    pluginConfig.IsMusicBrainzEnabled = bool.Parse(element.Value);
                    break;
                case "AlternativeListenDetectionEnabled":
                    pluginConfig.IsAlternativeModeEnabled = bool.Parse(element.Value);
                    break;
            }
        }

        return pluginConfig;
    }

    private static Collection<UserConfig> ParseUserConfigs(XContainer userConfigsArray)
    {
        return new Collection<UserConfig>(
            userConfigsArray
                .Descendants()
                .Where(c => c.Name.ToString() == "LbUser")
                .Select(ParseUserConfig)
                .ToList());
    }

    private static UserConfig ParseUserConfig(XElement config)
    {
        var newUserConfig = new UserConfig();
        foreach (var configField in config.Descendants())
        {
            switch (configField.Name.ToString())
            {
                case "Token":
                    newUserConfig.PlaintextApiToken = configField.Value;
                    break;
                case "MediaBrowserUserId":
                    newUserConfig.JellyfinUserId = new Guid(configField.Value);
                    break;
                case "ListenSubmitEnabled":
                    newUserConfig.IsListenSubmitEnabled = bool.Parse(configField.Value);
                    break;
                case "SyncFavoritesEnabled":
                    newUserConfig.IsFavoritesSyncEnabled = bool.Parse(configField.Value);
                    break;
            }
        }

        return newUserConfig;
    }
}
