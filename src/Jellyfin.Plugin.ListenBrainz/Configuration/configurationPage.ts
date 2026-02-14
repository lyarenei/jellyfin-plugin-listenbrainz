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
export default function (view: HTMLElement, _params: Record<string, string>): void {
    // TODO: set up hooks, fill settings, etc...
    console.log("Called init for config page");
}
