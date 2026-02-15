import { ConfigApiClient } from "../apiClient";
import { getUserConfig } from "../utils";
import { getUserConfigFormData } from "../formHelpers";

export function registerSubmitButtonHook(view: HTMLElement) {
    const configForm = view.querySelector("#ListenBrainzPluginConfigForm") as HTMLFormElement;
    configForm.addEventListener("submit", async (event) => {
        event.preventDefault();
        Dashboard.showLoadingMsg();

        const jellyfinUserDropdown = view.querySelector("#JellyfinUser") as HTMLSelectElement;
        const selectedUserId = jellyfinUserDropdown.value;

        try {
            const currentPluginConfig = await ConfigApiClient.getPluginConfiguration();

            switch (event.submitter?.id) {
                case "SaveUserConfig":
                    await saveUserConfig(view, currentPluginConfig, selectedUserId);
                    break;
                default:
                    console.warn("Unknown submit button clicked: " + event.submitter?.id);
                    Dashboard.alert("Unknown action");
            }
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to save configuration: " + e);
            Dashboard.alert("Failed to save configuration");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}

async function saveUserConfig(view: HTMLElement, currentPluginConfig: PluginConfiguration, selectedUserId: string) {
    const currentUserConfig = getUserConfig(currentPluginConfig, selectedUserId);
    const newUserConfig = getUserConfigFormData(view);
    const userApiToken = atob(newUserConfig.ApiToken);

    try {
        const validationResult = await ConfigApiClient.validateListenBrainzToken(userApiToken);
        newUserConfig.UserName = validationResult?.UserName || currentUserConfig.UserName;
    } catch {
        // We don't care if validation failed
    }

    // Update user config in the plugin config object
    const updatedPluginConfig: PluginConfiguration = {
        ...currentPluginConfig,
        UserConfigs: [
            ...currentPluginConfig.UserConfigs.filter((config) => config.JellyfinUserId !== selectedUserId),
            { ...newUserConfig },
        ],
    };

    const resp = await ConfigApiClient.savePluginConfiguration(updatedPluginConfig);
    Dashboard.processPluginConfigurationUpdateResult(resp);
}
