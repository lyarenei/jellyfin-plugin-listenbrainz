using System.Collections.ObjectModel;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of a PluginConfig service.
/// </summary>
public class DefaultPluginConfigService : IPluginConfigService
{
    private readonly Func<PluginConfiguration> _configAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPluginConfigService"/> class.
    /// </summary>
    /// <param name="configAccessor">Accessor returning the live plugin configuration.</param>
    public DefaultPluginConfigService(Func<PluginConfiguration> configAccessor)
    {
        _configAccessor = configAccessor;
    }

    private PluginConfiguration Config => _configAccessor();

    /// <inheritdoc />
    public bool IsAlternativeModeEnabled
    {
        get => Config.IsAlternativeModeEnabled;
    }

    /// <inheritdoc />
    public string ListenBrainzApiUrl
    {
        get => Config.ListenBrainzApiUrl;
    }

    /// <inheritdoc />
    public string MusicBrainzApiUrl
    {
        get => Config.MusicBrainzApiUrl;
    }

    /// <inheritdoc />
    public bool IsBackupEnabled
    {
        get => Config.IsBackupEnabled;
    }

    /// <inheritdoc />
    public bool IsMusicBrainzEnabled
    {
        get => Config.IsMusicBrainzEnabled;
    }

    /// <inheritdoc />
    public bool IsImmediateFavoriteSyncEnabled
    {
        get => Config.IsImmediateFavoriteSyncEnabled;
    }

    /// <inheritdoc />
    public bool IsAllPlaylistsSyncEnabled => Config.IsAllPlaylistsSyncEnabled;

    /// <inheritdoc />
    public Collection<LibraryConfig> LibraryConfigs
    {
        get => Config.LibraryConfigs;
    }

    /// <inheritdoc />
    public Collection<UserConfig> UserConfigs
    {
        get => Config.UserConfigs;
    }

    /// <inheritdoc />
    public string BackupPath => Config.BackupPath;

    /// <inheritdoc />
    public UserConfig? GetUserConfig(Guid jellyfinUserId)
    {
        var userConfig = Config
            .UserConfigs
            .FirstOrDefault(u => u.JellyfinUserId == jellyfinUserId);

        return userConfig;
    }
}
