interface AjaxOptions {
    contentType: string;
    data: string;
    dataType: string;
    type: string;
    url: string;
}

interface JellyfinApiClient {
    ajax(options: AjaxOptions): Promise<any>;
    getPluginConfiguration(pluginId: string): Promise<PluginConfiguration>;
    getUrl(path: string): Promise<string>;
    getUsers(): Promise<JellyfinUser[]>;
    updatePluginConfiguration(pluginId: string, config: PluginConfiguration): Promise<any>;
}

interface JellyfinDashboard {
    alert(message: string): void;
    hideLoadingMsg(): void;
    processPluginConfigurationUpdateResult(result: any): void;
    showLoadingMsg(): void;
}

interface JellyfinUser {
    Id: string;
    Name: string;
}

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

declare const ApiClient: JellyfinApiClient;
declare const Dashboard: JellyfinDashboard;
