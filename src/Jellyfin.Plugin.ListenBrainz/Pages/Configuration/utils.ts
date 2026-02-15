import { userDefaults } from "./constants";

/**
 * Retrieves the configuration for a specific user from the plugin configuration.
 * @param config - The overall plugin configuration containing the user configurations.
 * @param userId - The ID of the Jellyfin user for whom the configuration should be retrieved.
 * @return The user configuration object for the specified user, or a default configuration if none exists.
 */
export function getUserConfig(config: PluginConfiguration, userId: string): PluginUserConfig {
    return (
        config.UserConfigs.find((userConfig) => userConfig.JellyfinUserId === userId) || {
            ...userDefaults,
            JellyfinUserId: userId,
        }
    );
}

export function getUniqueLibraryName(libraryId: string): string {
    return `library_${libraryId}_IsAllowed`;
}
