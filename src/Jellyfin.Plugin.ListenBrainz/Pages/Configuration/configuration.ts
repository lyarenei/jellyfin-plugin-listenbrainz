import { ConfigApiClient } from "./apiClient";
import { userDefaults } from "./constants";
import { fillGeneralConfigForm, fillUserConfigForm } from "./formHelpers";
import registerEventHooks from "./eventHooks";

/**
 * Sets up the plugin config page. Should be only called once (when the page is first loaded).
 * @param view - The HTML element where the configuration page is rendered.
 * @return void
 */
export async function setUpPluginConfigPage(view: HTMLElement): Promise<void> {
    const jellyfinUsers = await ConfigApiClient.getUsers();
    buildUsersDropdown(view, jellyfinUsers);

    const jellyfinLibraries = await ConfigApiClient.getLibraries();
    buildLibrariesList(view, jellyfinLibraries);

    registerEventHooks(view);
}

export async function loadPluginConfigData(view: HTMLElement): Promise<void> {
    const pluginConfig = await ConfigApiClient.getPluginConfiguration();
    fillUserConfigForm(view, pluginConfig.UserConfigs[0] || userDefaults);
    fillGeneralConfigForm(view, pluginConfig);
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

function buildLibrariesList(view: HTMLElement, libraries: MediaLibrary[]) {
    const container = view.querySelector("#LibrariesList") as HTMLDivElement;

    libraries.forEach((library) => {
        const label = document.createElement("label");
        label.classList.add("inputLabel", "inputLabelUnfocused");
        label.htmlFor = getUniqueName(library);
        label.onclick = (_) => {
            const checkbox = document.getElementById(library.Id) as HTMLInputElement;
            checkbox.checked = !checkbox.checked;
        };

        const checkbox = document.createElement("input");
        checkbox.setAttribute("is", "emby-checkbox");
        checkbox.type = "checkbox";
        checkbox.id = getUniqueName(library);
        checkbox.name = getUniqueName(library);

        const span = document.createElement("span");
        span.textContent = library.Name;

        label.appendChild(checkbox);
        label.appendChild(span);
        container.appendChild(label);
    });
}

function getUniqueName(library: MediaLibrary): string {
    return `library_${library.Id}_IsAllowed`;
}
