import { ConfigApiClient } from "../apiClient";
import { getUserConfig } from "../utils";
import {
    getBackupConfigFormData,
    getGeneralConfigFormData,
    getLibrariesConfigFormData,
    getMusicBrainzConfigFormData,
    getUserConfigFormData,
} from "../formHelpers";
import { PluginConfiguration } from "../types";

export function registerUserConfigSubmitHook(view: HTMLElement) {
    const form = view.querySelector("#UserConfigForm") as HTMLFormElement;
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        Dashboard.showLoadingMsg();

        const jellyfinUserDropdown = view.querySelector("#JellyfinUser") as HTMLSelectElement;
        const selectedUserId = jellyfinUserDropdown.value;

        try {
            const currentPluginConfig = await ConfigApiClient.getPluginConfiguration();
            await saveUserConfig(view, currentPluginConfig, selectedUserId);
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to save user configuration: " + JSON.stringify(e));
            Dashboard.alert("Failed to save user configuration");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}

export function registerGeneralConfigSubmitHook(view: HTMLElement) {
    const form = view.querySelector("#GeneralConfigForm") as HTMLFormElement;
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        Dashboard.showLoadingMsg();

        try {
            const currentPluginConfig = await ConfigApiClient.getPluginConfiguration();
            const newGeneralConfig = getGeneralConfigFormData(view);
            const updatedPluginConfig: PluginConfiguration = {
                ...currentPluginConfig,
                ...newGeneralConfig,
            };

            const resp = await ConfigApiClient.savePluginConfiguration(updatedPluginConfig);
            Dashboard.processPluginConfigurationUpdateResult(resp);
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to save general configuration: " + JSON.stringify(e));
            Dashboard.alert("Failed to save general configuration");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}

export function registerMusicBrainzConfigSubmitHook(view: HTMLElement) {
    const form = view.querySelector("#MusicBrainzConfigForm") as HTMLFormElement;
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        Dashboard.showLoadingMsg();

        try {
            const currentPluginConfig = await ConfigApiClient.getPluginConfiguration();
            const newMusicBrainzConfig = getMusicBrainzConfigFormData(view);
            const updatedPluginConfig: PluginConfiguration = {
                ...currentPluginConfig,
                ...newMusicBrainzConfig,
            };

            const resp = await ConfigApiClient.savePluginConfiguration(updatedPluginConfig);
            Dashboard.processPluginConfigurationUpdateResult(resp);
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to save MusicBrainz configuration: " + JSON.stringify(e));
            Dashboard.alert("Failed to save MusicBrainz configuration");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}

export function registerBackupConfigSubmitHook(view: HTMLElement) {
    const form = view.querySelector("#BackupConfigForm") as HTMLFormElement;
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        Dashboard.showLoadingMsg();

        try {
            const currentPluginConfig = await ConfigApiClient.getPluginConfiguration();
            const newBackupConfig = getBackupConfigFormData(view);
            const updatedPluginConfig: PluginConfiguration = {
                ...currentPluginConfig,
                ...newBackupConfig,
            };

            const resp = await ConfigApiClient.savePluginConfiguration(updatedPluginConfig);
            Dashboard.processPluginConfigurationUpdateResult(resp);
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to save backup configuration: " + JSON.stringify(e));
            Dashboard.alert("Failed to save backup configuration");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}

export function registerLibrariesConfigSubmitHook(view: HTMLElement) {
    const form = view.querySelector("#LibrariesConfigForm") as HTMLFormElement;
    form.addEventListener("submit", async (event) => {
        event.preventDefault();
        Dashboard.showLoadingMsg();

        try {
            const currentPluginConfig = await ConfigApiClient.getPluginConfiguration();
            const newLibrariesConfig = getLibrariesConfigFormData(view);
            const updatedPluginConfig: PluginConfiguration = {
                ...currentPluginConfig,
                ...newLibrariesConfig,
            };

            const resp = await ConfigApiClient.savePluginConfiguration(updatedPluginConfig);
            Dashboard.processPluginConfigurationUpdateResult(resp);
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to save libraries configuration: " + JSON.stringify(e));
            Dashboard.alert("Failed to save libraries configuration");
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

    const updatedUserConfig = {
        ...currentUserConfig,
        ...newUserConfig,
        PlaylistMappings: currentUserConfig.PlaylistMappings ?? [],
    };

    const updatedPluginConfig: PluginConfiguration = {
        ...currentPluginConfig,
        UserConfigs: [
            ...currentPluginConfig.UserConfigs.filter((config) => config.JellyfinUserId !== selectedUserId),
            updatedUserConfig,
        ],
    };

    const resp = await ConfigApiClient.savePluginConfiguration(updatedPluginConfig);
    Dashboard.processPluginConfigurationUpdateResult(resp);
}
