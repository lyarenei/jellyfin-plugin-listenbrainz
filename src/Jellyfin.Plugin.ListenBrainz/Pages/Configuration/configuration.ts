import { ConfigApiClient } from "./apiClient";
import { userDefaults } from "./constants";
import { fillUserConfigForm } from "./formHelpers";
import registerEventHooks from "./eventHooks";

/**
 * Sets up the plugin config page. Should be only called once (when the page is first loaded).
 * @param view - The HTML element where the configuration page is rendered.
 * @return void
 */
export async function setUpPluginConfigPage(view: HTMLElement): Promise<void> {
    const jellyfinUsers = await ConfigApiClient.getUsers();
    buildUsersDropdown(view, jellyfinUsers);
    registerEventHooks(view);
}

export async function loadPluginConfigData(view: HTMLElement): Promise<void> {
    const pluginConfig = await ConfigApiClient.getPluginConfiguration();
    fillUserConfigForm(view, pluginConfig.UserConfigs[0] || userDefaults);
}

function buildUsersDropdown(view: HTMLElement, users: JellyfinUser[]) {
    const dropdown = view.querySelector("#JellyfinUser") as HTMLSelectElement;

    users.forEach((user) => {
        const option = document.createElement("option");
        option.value = user.Id;
        option.textContent = user.Name;
        dropdown.appendChild(option);
    });
}
