import { ConfigApiClient } from "./apiClient";

export function registerEventHooks(view: HTMLElement) {
    registerApiTokenCheckButtonHook(view);
}

function registerApiTokenCheckButtonHook(view: HTMLElement) {
    const apiTokenCheckButton = view.querySelector("#CheckListenBrainzApiToken") as HTMLButtonElement;
    apiTokenCheckButton.addEventListener("click", async () => {
        const apiTokenInput = view.querySelector("#ListenBrainzApiToken") as HTMLInputElement;
        const apiToken = apiTokenInput.value.trim();

        if (!apiToken) {
            Dashboard.alert("Please enter an API token");
            return;
        }

        Dashboard.showLoadingMsg();
        try {
            const validationResult = await ConfigApiClient.validateListenBrainzToken(apiToken);
            if (validationResult.IsValid) {
                Dashboard.alert("API token is valid! Associated ListenBrainz user: " + validationResult.UserName);
            } else {
                Dashboard.alert("API token is invalid: " + validationResult.Reason);
            }
        } catch (e) {
            console.log("ListenBrainz plugin: Failed to validate API token: " + e);
            Dashboard.alert("Failed to validate API token");
        } finally {
            Dashboard.hideLoadingMsg();
        }
    });
}
