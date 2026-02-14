/**
 * ID of the plugin. Must never change, otherwise it will be treated by the server as a new plugin.
 * Must be the same as the ID in Plugin.cs file and in the repository manifest file.
 */
const pluginUUID = "59B20823-AAFE-454C-A393-17427F518631";

/**
 * Default values for the plugin user configuration.
 * The keys must match the keys of the user configuration model defined in UserConfig.cs file.
 */
const userDefaults = {
    JellyfinUserId: "",
    ApiToken: "",
    IsListenSubmitEnabled: false,
    IsFavoritesSyncEnabled: false,
    IsPlaylistsSyncEnabled: false,
    IsAllPlaylistsSyncEnabled: false,
    IsUserBackupEnabled: false,
    IsStrictModeEnabled: false,
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
    dropdown.addEventListener("change", (event) => {
        const selectedUserId = (event.target as HTMLSelectElement).value;
        loadUserConfig(view, selectedUserId);
    });
}

function loadUserConfig(view: HTMLElement, selectedUserId: string) {
    // TODO: implementation
}
