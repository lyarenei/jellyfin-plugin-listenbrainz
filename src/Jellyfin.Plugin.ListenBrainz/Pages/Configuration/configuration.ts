import { ConfigApiClient } from "./apiClient";
import {
    fillBackupConfigForm,
    fillGeneralConfigForm,
    fillLibrariesConfigForm,
    fillMusicBrainzConfigForm,
    fillUserConfigForm,
} from "./formHelpers";
import registerEventHooks from "./eventHooks";
import { initTabs } from "./tabs";
import { getUniqueLibraryName, getUserConfig } from "./utils";
import { MediaLibrary } from "./types";

/**
 * Sets up the plugin config page. Should be only called once (when the page is first loaded).
 * @param view - The HTML element where the configuration page is rendered.
 * @return void
 */
export async function setUpPluginConfigPage(view: HTMLElement): Promise<void> {
    initTabs(view);

    const jellyfinUsers = await ConfigApiClient.getUsers();
    buildUsersDropdown(view, jellyfinUsers);

    const jellyfinLibraries = await ConfigApiClient.getLibraries();
    buildLibrariesList(view, jellyfinLibraries);

    registerEventHooks(view);
}

export async function loadPluginConfigData(view: HTMLElement): Promise<void> {
    const pluginConfig = await ConfigApiClient.getPluginConfiguration();
    const jellyfinLibraries = await ConfigApiClient.getLibraries();
    const userDropdown = view.querySelector("#JellyfinUser") as HTMLSelectElement;
    const userConfig = getUserConfig(pluginConfig, userDropdown.value);

    fillUserConfigForm(view, userConfig);
    fillGeneralConfigForm(view, pluginConfig);
    fillMusicBrainzConfigForm(view, pluginConfig);
    fillBackupConfigForm(view, pluginConfig);
    fillLibrariesConfigForm(view, pluginConfig, jellyfinLibraries);
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
        label.htmlFor = getUniqueLibraryName(library.Id);

        const checkbox = document.createElement("input");
        checkbox.setAttribute("is", "emby-checkbox");
        checkbox.type = "checkbox";
        checkbox.id = getUniqueLibraryName(library.Id);
        checkbox.name = getUniqueLibraryName(library.Id);
        checkbox.dataset.musicLibrary = String(library.IsMusicLibrary);

        const span = document.createElement("span");
        span.textContent = library.Name;

        label.appendChild(checkbox);
        label.appendChild(span);
        container.appendChild(label);
    });
}
