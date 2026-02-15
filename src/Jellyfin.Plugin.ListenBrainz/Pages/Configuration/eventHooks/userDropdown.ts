import { ConfigApiClient } from "../apiClient";
import { getUserConfig } from "../utils";
import { fillUserConfigForm } from "../formHelpers";

export function registerUserDropdownChangeHook(view: HTMLElement) {
    const dropdown = view.querySelector("#JellyfinUser") as HTMLSelectElement;
    dropdown.addEventListener("change", async (event) => {
        Dashboard.showLoadingMsg();
        try {
            const selectedUserId = (event.target as HTMLSelectElement).value;
            await loadUserConfig(view, selectedUserId);
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to load user configuration: " + e);
            Dashboard.alert("Failed to load user configuration");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}

async function loadUserConfig(view: HTMLElement, selectedUserId: string) {
    const pluginConfig = await ConfigApiClient.getPluginConfiguration();
    const userConfig = getUserConfig(pluginConfig, selectedUserId);
    fillUserConfigForm(view, userConfig);
}
