import { getUniqueLibraryName } from "./utils";

function getUserConfigFormElements(view: HTMLElement) {
    return {
        apiToken: view.querySelector("#ListenBrainzApiToken") as HTMLInputElement,
        backup: view.querySelector("#IsUserBackupEnabled") as HTMLInputElement,
        favoritesSync: view.querySelector("#IsFavoritesSyncEnabled") as HTMLInputElement,
        listenSubmit: view.querySelector("#IsListenSubmitEnabled") as HTMLInputElement,
        playlistsSync: view.querySelector("#IsPlaylistsSyncEnabled") as HTMLInputElement,
        strictMode: view.querySelector("#IsStrictModeEnabled") as HTMLInputElement,
        userDropdown: view.querySelector("#JellyfinUser") as HTMLSelectElement,
    };
}

function getGeneralConfigFormElements(view: HTMLElement) {
    return {
        allPlaylistsEnabled: view.querySelector("#IsAllPlaylistsSyncEnabled") as HTMLInputElement,
        altModeEnabled: view.querySelector("#IsAlternativeModeEnabled") as HTMLInputElement,
        backupPath: view.querySelector("#BackupPath") as HTMLInputElement,
        immediateFavorites: view.querySelector("#IsImmediateFavoriteSyncEnabled") as HTMLInputElement,
        listenBrainzUrl: view.querySelector("#ListenBrainzApiUrl") as HTMLInputElement,
        musicBrainzEnabled: view.querySelector("#IsMusicBrainzEnabled") as HTMLInputElement,
        musicBrainzUrl: view.querySelector("#MusicBrainzApiUrl") as HTMLInputElement,
    };
}

export function fillUserConfigForm(view: HTMLElement, userConfig: PluginUserConfig): void {
    const elements = getUserConfigFormElements(view);
    elements.apiToken.value = atob(userConfig.ApiToken);
    elements.backup.checked = userConfig.IsBackupEnabled;
    elements.favoritesSync.checked = userConfig.IsFavoritesSyncEnabled;
    elements.listenSubmit.checked = userConfig.IsListenSubmitEnabled;
    elements.playlistsSync.checked = userConfig.IsPlaylistsSyncEnabled;
    elements.strictMode.checked = userConfig.IsStrictModeEnabled;
}

export function getUserConfigFormData(view: HTMLElement): PluginUserConfig {
    const elements = getUserConfigFormElements(view);
    return {
        ApiToken: btoa(elements.apiToken.value.trim()),
        IsBackupEnabled: elements.backup.checked,
        IsFavoritesSyncEnabled: elements.favoritesSync.checked,
        IsListenSubmitEnabled: elements.listenSubmit.checked,
        IsPlaylistsSyncEnabled: elements.playlistsSync.checked,
        IsStrictModeEnabled: elements.strictMode.checked,
        JellyfinUserId: elements.userDropdown.value,
        UserName: "",
    };
}

export function fillGeneralConfigForm(
    view: HTMLElement,
    pluginConfig: PluginConfiguration,
    jellyfinLibraries: MediaLibrary[],
): void {
    const elements = getGeneralConfigFormElements(view);
    elements.allPlaylistsEnabled.checked = pluginConfig.IsAllPlaylistsSyncEnabled;
    elements.altModeEnabled.checked = pluginConfig.IsAlternativeModeEnabled;
    elements.backupPath.value = pluginConfig.BackupPath;
    elements.immediateFavorites.checked = pluginConfig.IsImmediateFavoriteSyncEnabled;
    elements.listenBrainzUrl.value = pluginConfig.ListenBrainzApiUrl;
    elements.musicBrainzEnabled.checked = pluginConfig.IsMusicBrainzEnabled;
    elements.musicBrainzUrl.value = pluginConfig.MusicBrainzApiUrl;

    if (pluginConfig.LibraryConfigs.length > 0) {
        pluginConfig.LibraryConfigs.map((lc) => {
            const checkboxId = getUniqueLibraryName(lc.Id);
            const checkbox = view.querySelector(`#${checkboxId}`) as HTMLInputElement;
            if (checkbox) {
                checkbox.checked = lc.IsAllowed;
            }
        });

        return;
    }

    jellyfinLibraries.forEach((library) => {
        const checkboxId = getUniqueLibraryName(library.Id);
        const checkbox = view.querySelector(`#${checkboxId}`) as HTMLInputElement;
        if (checkbox) {
            checkbox.checked = library.IsMusicLibrary;
        }
    });
}
