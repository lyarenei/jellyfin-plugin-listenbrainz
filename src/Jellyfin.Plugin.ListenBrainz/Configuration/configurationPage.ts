/**
 * ID of the plugin. Must be the same as the ID in Plugin.cs file.
 */
const pluginUUID = "59B20823-AAFE-454C-A393-17427F518631";

/**
 * Default values for the plugin user configuration.
 * The keys must match the keys of the user configuration model defined in UserConfig.cs file.
 */
const userDefaults: PluginUserConfig = {
    ApiToken: "",
    IsFavoritesSyncEnabled: false,
    IsListenSubmitEnabled: false,
    IsPlaylistsSyncEnabled: false,
    IsStrictModeEnabled: false,
    IsBackupEnabled: false,
    JellyfinUserId: "",
    UserName: "",
};

/**
 * This function acts as a constructor and is called when the configuration page is loaded.
 * It receives the HTML element where the configuration page should be rendered and any parameters passed to it.
 *
 * @param view - The HTML element where the configuration page should be rendered.
 * @param _params - A record of parameters passed to the configuration page.
 * @return void
 */
export default function (view: HTMLElement, _params: Record<string, string>) {
    // This function cannot be async, so instead hook into the viewshow event to call async functions.
    view.addEventListener('viewshow', async () => {
        Dashboard.showLoadingMsg();
        try {
            await initPluginConfigPage(view);
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to initialize configuration page: " + e);
            Dashboard.alert("Failed to initialize configuration page");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}

/**
 * Initializes the plugin configuration page by loading the necessary data and populating the form fields.
 *
 * @param view - The HTML element where the configuration page is rendered.
 * @return void
 */
async function initPluginConfigPage(view: HTMLElement): Promise<void> {
    const pluginConfig = await ApiClient.getPluginConfiguration(pluginUUID);
    const jellyfinUsers = await ApiClient.getUsers();

    buildUsersDropdown(view, jellyfinUsers);
    fillUserConfigForm(view, pluginConfig.UserConfigs[0] || userDefaults);
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
    const pluginConfig = await ApiClient.getPluginConfiguration(pluginUUID);

    // Find the config or use defaults if it doesn't exist yet (e.g. when selecting a user for the first time)
    const userConfig = pluginConfig.UserConfigs.find((config) =>
        config.JellyfinUserId === selectedUserId) || {
        ...userDefaults,
        JellyfinUserId: selectedUserId
    };

    fillUserConfigForm(view, userConfig);
}

/**
 * Fills the configuration form fields with the values from the provided user configuration.
 * @param view - The HTML element where the configuration page is rendered.
 * @param userConfig - The user configuration object containing the values to fill in the form fields.
 * @return void
 */
function fillUserConfigForm(view: HTMLElement, userConfig: PluginUserConfig): void {
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
