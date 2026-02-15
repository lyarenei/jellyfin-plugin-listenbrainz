interface PluginConfiguration {
    UserConfigs: PluginUserConfig[];
}

// Plugin user configuration.
// The keys must match the fields of UserConfig class.
interface PluginUserConfig {
    ApiToken: string;
    IsBackupEnabled: boolean;
    IsFavoritesSyncEnabled: boolean;
    IsListenSubmitEnabled: boolean;
    IsPlaylistsSyncEnabled: boolean;
    IsStrictModeEnabled: boolean;
    JellyfinUserId: string;
    UserName: string;
}

interface TokenValidationResult {
    IsValid: boolean;
    Reason: string;
    UserName: string;
}
