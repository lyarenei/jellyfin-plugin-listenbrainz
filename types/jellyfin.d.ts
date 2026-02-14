interface JellyfinApiClient {
    getPluginConfiguration(pluginId: string): Promise<PluginConfiguration>;
    getUsers(): Promise<JellyfinUser[]>;
}

interface JellyfinDashboard {
    alert(message: string): void;
    hideLoadingMsg(): void;
    showLoadingMsg(): void;
}

interface JellyfinUser {
    Id: string;
    Name: string;
}

interface PluginConfiguration {
    UserConfigs: UserConfig[];
}

interface PluginUserConfig {
    JellyfinUserId: string;
    UserName: string;
}

declare const ApiClient: JellyfinApiClient;
declare const Dashboard: JellyfinDashboard;
