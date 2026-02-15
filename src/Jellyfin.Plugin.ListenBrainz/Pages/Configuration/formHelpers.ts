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
