using Jellyfin.Plugin.ListenBrainz.Configuration;

namespace Jellyfin.Plugin.ListenBrainz.Interfaces;

/// <summary>
/// A service for accessing plugin configuration.
/// </summary>
public interface IPluginConfigService
{
    /// <summary>
    /// Gets a value indicating whether the alternative mode is enabled.
    /// </summary>
    public bool IsAlternativeModeEnabled { get; }

    /// <summary>
    /// Gets configured ListenBrainz API URL.
    /// </summary>
    /// <returns>ListenBrainz API URL.</returns>
    public string ListenBrainzApiUrl { get; }

    /// <summary>
    /// Gets a value indicating whether listen backup feature is enabled.
    /// </summary>
    bool IsBackupEnabled { get; }

    /// <summary>
    /// Gets a value indicating whether MusicBrainz integration is enabled.
    /// </summary>
    bool IsMusicBrainzEnabled { get; }

    /// <summary>
    /// Get a configuration for a specified Jellyfin user ID.
    /// </summary>
    /// <param name="jellyfinUserId">ID of the Jellyfin user.</param>
    /// <returns>User configuration. Null if it does not exist.</returns>
    public UserConfig? GetUserConfig(Guid jellyfinUserId);
}
