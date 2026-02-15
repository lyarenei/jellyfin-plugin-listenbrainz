interface AjaxOptions {
    contentType: string;
    data?: string;
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

declare const ApiClient: JellyfinApiClient;
declare const Dashboard: JellyfinDashboard;
