import {initPluginConfigPage} from "./configuration";

/**
 * This function server as an entrypoint and is called when the configuration page is loaded.
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
