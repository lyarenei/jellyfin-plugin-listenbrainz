interface LibraryConfig {
    Id: string;
    Name: string;
    IsAllowed: boolean;
}

interface MediaLibrary {
    Id: string;
    Name: string;
    IsMusicLibrary: boolean;
}

interface PluginConfiguration {
    BackupPath: string;
    IsAllPlaylistsSyncEnabled: boolean;
    IsAlternativeModeEnabled: boolean;
    IsImmediateFavoriteSyncEnabled: boolean;
    IsMusicBrainzEnabled: boolean;
    LibraryConfigs: LibraryConfig[];
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
