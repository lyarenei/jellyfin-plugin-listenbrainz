import { pluginUUID } from "./constants";
import { MediaLibrary, PluginConfiguration, TokenValidationResult } from "./types";

export const ConfigApiClient = {
    ajax: (options: AjaxOptions): Promise<unknown> => {
        return ApiClient.ajax(options);
    },
    getUrl: (path: string): Promise<string> => {
        return ApiClient.getUrl(path);
    },
    getLibraries: async (): Promise<MediaLibrary[]> => {
        try {
            const url = await ConfigApiClient.getUrl("ListenBrainzPlugin/internal/libraries");
            const response = await ConfigApiClient.ajax({
                contentType: "application/json",
                dataType: "json",
                type: "GET",
                url: url,
            });

            return response as MediaLibrary[];
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to get libraries: " + e);
            throw new Error("Failed to get libraries");
        }
    },
    getPluginConfiguration: (): Promise<PluginConfiguration> => {
        return ApiClient.getPluginConfiguration(pluginUUID);
    },
    getUsers: (): Promise<JellyfinUser[]> => {
        return ApiClient.getUsers();
    },
    savePluginConfiguration: (newPluginConfig: PluginConfiguration): Promise<object> => {
        return ApiClient.updatePluginConfiguration(pluginUUID, newPluginConfig);
    },
    validateListenBrainzToken: async (apiToken: string): Promise<TokenValidationResult> => {
        try {
            const url = await ConfigApiClient.getUrl("/ListenBrainzPlugin/ValidateToken");
            const response = await ConfigApiClient.ajax({
                contentType: "application/json",
                data: JSON.stringify(apiToken),
                dataType: "json",
                type: "POST",
                url: url,
            });

            return response as TokenValidationResult;
        } catch (e) {
            console.log("ListenBrainz plugin: Error validating API token: " + e);
            throw new Error("Failed to validate API token");
        }
    },
};
