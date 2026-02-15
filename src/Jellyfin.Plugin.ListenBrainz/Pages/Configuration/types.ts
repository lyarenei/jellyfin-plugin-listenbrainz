interface PluginConfiguration {
    BackupPath: string;
    IsAllPlaylistsSyncEnabled: boolean;
    IsAlternativeModeEnabled: boolean;
    IsImmediateFavoriteSyncEnabled: boolean;
    IsMusicBrainzEnabled: boolean;
    ListenBrainzApiUrl: string;
    MusicBrainzApiUrl: string;
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
