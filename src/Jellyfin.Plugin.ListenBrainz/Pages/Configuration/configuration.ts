import { ConfigApiClient } from "./apiClient";
import { pluginUUID, userDefaults } from "./constants";
import { registerEventHooks } from "./eventHooks";
import { fillUserConfigForm } from "./formHelpers";
import { getUserConfig } from "./utils";

/**
 * Initializes the plugin configuration page by loading the necessary data and populating the form fields.
 *
 * @param view - The HTML element where the configuration page is rendered.
 * @return void
 */
export async function initPluginConfigPage(view: HTMLElement): Promise<void> {
    const pluginConfig = await ConfigApiClient.getPluginConfiguration(pluginUUID);
    const jellyfinUsers = await ConfigApiClient.getUsers();

    buildUsersDropdown(view, jellyfinUsers);
    fillUserConfigForm(view, pluginConfig.UserConfigs[0] || userDefaults);
    registerEventHooks(view);
}

/**
 * Builds the users dropdown in the configuration page and sets up the event listener for user selection.
 * @param view - The HTML element where the configuration page is rendered.
 * @param users - An array of Jellyfin users to populate the dropdown with.
 * @return void
 */
function buildUsersDropdown(view: HTMLElement, users: JellyfinUser[]) {
    const dropdown = view.querySelector("#JellyfinUser") as HTMLSelectElement;

    users.forEach((user) => {
        const option = document.createElement("option");
        option.value = user.Id;
        option.textContent = user.Name;
        dropdown.appendChild(option);
    });

    // Load the config when a user is selected
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

/**
 * Loads the configuration for the selected user and fills the form fields with the corresponding values.
 * @param view - The HTML element where the configuration page is rendered.
 * @param selectedUserId - The ID of the selected Jellyfin user for whom the configuration should be loaded.
 * @return void
 */
async function loadUserConfig(view: HTMLElement, selectedUserId: string) {
    const pluginConfig = await ConfigApiClient.getPluginConfiguration(pluginUUID);
    const userConfig = getUserConfig(pluginConfig, selectedUserId);
    fillUserConfigForm(view, userConfig);
}
