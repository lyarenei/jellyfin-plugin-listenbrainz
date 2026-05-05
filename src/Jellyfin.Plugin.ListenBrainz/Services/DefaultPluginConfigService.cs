using System.Collections.ObjectModel;
using Jellyfin.Plugin.ListenBrainz.Configuration;
using Jellyfin.Plugin.ListenBrainz.Interfaces;

namespace Jellyfin.Plugin.ListenBrainz.Services;

/// <summary>
/// Default implementation of a PluginConfig service.
/// </summary>
public class DefaultPluginConfigService : IPluginConfigService
{
    private static PluginConfiguration Config => Plugin.GetConfiguration();

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

    /// <inheritdoc />
    public Guid? GetPlaylistId(Guid jellyfinUserId, string playlistId)
    {
        var userConfig = GetUserConfig(jellyfinUserId);
        var mapping = userConfig?
            .PlaylistMappings
            .FirstOrDefault(m => string.Equals(m.ListenBrainzPlaylistId, playlistId, StringComparison.Ordinal));

        return mapping?.JellyfinPlaylistId;
    }

    /// <inheritdoc />
    public bool SetPlaylistMapping(Guid jellyfinUserId, string listenBrainzPlaylistId, Guid jellyfinPlaylistId)
    {
        var userConfig = GetUserConfig(jellyfinUserId);
        if (userConfig is null)
        {
            return false;
        }

        var mapping = userConfig
            .PlaylistMappings
            .FirstOrDefault(m =>
                string.Equals(m.ListenBrainzPlaylistId, listenBrainzPlaylistId, StringComparison.Ordinal));

        if (mapping is null)
        {
            userConfig.PlaylistMappings.Add(new PlaylistMapping
            {
                ListenBrainzPlaylistId = listenBrainzPlaylistId,
                JellyfinPlaylistId = jellyfinPlaylistId,
            });
        }
        else if (mapping.JellyfinPlaylistId == jellyfinPlaylistId)
        {
            return true;
        }
        else
        {
            mapping.JellyfinPlaylistId = jellyfinPlaylistId;
        }

        Plugin.UpdateConfig(Config);
        return true;
    }
}
