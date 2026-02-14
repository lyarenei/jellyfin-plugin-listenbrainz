export const ConfigApiClient = {
    getPluginConfiguration: async (pluginUUID: string): Promise<PluginConfiguration> => {
        return ApiClient.getPluginConfiguration(pluginUUID);
    },
    getUsers: async (): Promise<JellyfinUser[]> => {
        return ApiClient.getUsers();
    },
    validateListenBrainzToken: async (apiToken: string): Promise<TokenValidationResult> => {
        try {
            const url = await ApiClient.getUrl("/ListenBrainzPlugin/ValidateToken");
            const response = await ApiClient.ajax({
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
