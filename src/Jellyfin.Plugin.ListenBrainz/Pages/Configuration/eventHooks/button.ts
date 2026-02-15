import { defaultListenBrainzApiUrl, defaultMusicBrainzApiUrl } from "../constants";

export function registerResetListenBrainzApiUrlButtonHook(view: HTMLElement) {
    const resetButton = view.querySelector("#ResetListenBrainzApiUrl") as HTMLButtonElement;
    resetButton.addEventListener("click", async () => {
        const apiUrlInput = view.querySelector("#ListenBrainzApiUrl") as HTMLInputElement;
        apiUrlInput.value = defaultListenBrainzApiUrl;
    });
}

export function registerResetMusicBrainzApiUrlButtonHook(view: HTMLElement) {
    const resetButton = view.querySelector("#ResetMusicBrainzApiUrl") as HTMLButtonElement;
    resetButton.addEventListener("click", async () => {
        const apiUrlInput = view.querySelector("#MusicBrainzApiUrl") as HTMLInputElement;
        apiUrlInput.value = defaultMusicBrainzApiUrl;
    });
}
