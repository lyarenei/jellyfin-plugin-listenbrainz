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

export function registerBackupPathBrowserButtonHook(view: HTMLElement) {
    const browseButton = view.querySelector("#SetBackupPath") as HTMLButtonElement;
    browseButton.addEventListener("click", async () => {
        const backupPathInput = view.querySelector("#BackupPath") as HTMLInputElement;
        const directoryBrowser = new Dashboard.DirectoryBrowser();
        directoryBrowser.show({
            callback: (selectedPath) => {
                directoryBrowser.close();
                backupPathInput.value = selectedPath;
            },
            header: "Select backup directory",
            includeFiles: false,
        });
    });
}
