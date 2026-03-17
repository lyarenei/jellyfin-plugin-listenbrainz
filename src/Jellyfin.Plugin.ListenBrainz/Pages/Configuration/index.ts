import { setUpPluginConfigPage, loadPluginConfigData } from "./configuration";
import { ConfigApiClient } from "./apiClient";

async function loadCss(pageName: string): Promise<void> {
    if (document.querySelector(`style[data-lb-css="${pageName}"]`)) {
        return;
    }

    const url = await ConfigApiClient.getUrl(`web/configurationpage?name=${pageName}`);
    const cssContent = await ConfigApiClient.ajax({
        contentType: "text/css",
        dataType: "text",
        type: "GET",
        url: url,
    });

    const styleTag = document.createElement("style");
    styleTag.setAttribute("data-lb-css", pageName);
    styleTag.textContent = cssContent as string;
    document.head.appendChild(styleTag);
}

async function loadStyles(): Promise<void> {
    await loadCss("ListenBrainz.styles.css");
}

export default function (view: HTMLElement, _params: Record<string, string>) {
    let isSetUp = false;

    // This function cannot be async, so instead hook into the viewshow event to call async functions.
    view.addEventListener("viewshow", async () => {
        Dashboard.showLoadingMsg();
        try {
            if (!isSetUp) {
                loadStyles().catch((e) => {
                    console.warn("ListenBrainz plugin: Failed to load configuration page styles:", e);
                });
                await setUpPluginConfigPage(view);
                isSetUp = true;
            }

            await loadPluginConfigData(view);
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to initialize configuration page: " + e);
            Dashboard.alert("Failed to initialize configuration page");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}
