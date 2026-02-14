interface JellyfinApiClient {
    getPluginConfiguration(pluginId: string): Promise<PluginConfiguration>;
}

interface JellyfinDashboard {
    alert(message: string): void;
    hideLoadingMsg(): void;
    showLoadingMsg(): void;
}

interface PluginConfiguration {
    
}

declare const ApiClient: JellyfinApiClient;
declare const Dashboard: JellyfinDashboard;
