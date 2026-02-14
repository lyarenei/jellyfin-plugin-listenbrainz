/**
 * Fills the configuration form fields with the values from the provided user configuration.
 * @param view - The HTML element where the configuration page is rendered.
 * @param userConfig - The user configuration object containing the values to fill in the form fields.
 * @return void
 */
export function fillUserConfigForm(view: HTMLElement, userConfig: PluginUserConfig): void {
    const apiTokenInput = view.querySelector("#ListenBrainzApiToken") as HTMLInputElement;
    apiTokenInput.value = atob(userConfig.ApiToken);

    const userBackupCheckbox = view.querySelector("#IsUserBackupEnabled") as HTMLInputElement;
    userBackupCheckbox.checked = userConfig.IsBackupEnabled;

    const favoritesSyncCheckbox = view.querySelector("#IsFavoritesSyncEnabled") as HTMLInputElement;
    favoritesSyncCheckbox.checked = userConfig.IsFavoritesSyncEnabled;

    const listenSubmitCheckbox = view.querySelector("#IsListenSubmitEnabled") as HTMLInputElement;
    listenSubmitCheckbox.checked = userConfig.IsListenSubmitEnabled;

    const playlistsSyncCheckbox = view.querySelector("#IsPlaylistsSyncEnabled") as HTMLInputElement;
    playlistsSyncCheckbox.checked = userConfig.IsPlaylistsSyncEnabled;

    const strictModeCheckbox = view.querySelector("#IsStrictModeEnabled") as HTMLInputElement;
    strictModeCheckbox.checked = userConfig.IsStrictModeEnabled;
}

/**
 * Collects the values from the configuration form fields.
 * @param view - The HTML element where the configuration page is rendered.
 * @return A PluginUserConfig object containing the values from the form fields.
 */
export function getUserConfigFormData(view: HTMLElement): PluginUserConfig {
    const apiTokenInput = view.querySelector("#ListenBrainzApiToken") as HTMLInputElement;
    const userBackupCheckbox = view.querySelector("#IsUserBackupEnabled") as HTMLInputElement;
    const favoritesSyncCheckbox = view.querySelector("#IsFavoritesSyncEnabled") as HTMLInputElement;
    const listenSubmitCheckbox = view.querySelector("#IsListenSubmitEnabled") as HTMLInputElement;
    const playlistsSyncCheckbox = view.querySelector("#IsPlaylistsSyncEnabled") as HTMLInputElement;
    const strictModeCheckbox = view.querySelector("#IsStrictModeEnabled") as HTMLInputElement;
    const jellyfinUserDropdown = view.querySelector("#JellyfinUser") as HTMLSelectElement;

    return {
        ApiToken: btoa(apiTokenInput.value.trim()),
        IsBackupEnabled: userBackupCheckbox.checked,
        IsFavoritesSyncEnabled: favoritesSyncCheckbox.checked,
        IsListenSubmitEnabled: listenSubmitCheckbox.checked,
        IsPlaylistsSyncEnabled: playlistsSyncCheckbox.checked,
        IsStrictModeEnabled: strictModeCheckbox.checked,
        JellyfinUserId: jellyfinUserDropdown.value,
        UserName: "",
    };
}
