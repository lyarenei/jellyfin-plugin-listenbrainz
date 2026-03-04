import { registerUserDropdownChangeHook } from "./userDropdown";
import { registerApiTokenCheckButtonHook } from "./apiToken";
import {
    registerBackupConfigSubmitHook,
    registerGeneralConfigSubmitHook,
    registerLibrariesConfigSubmitHook,
    registerMusicBrainzConfigSubmitHook,
    registerUserConfigSubmitHook,
} from "./submit";
import {
    registerBackupPathBrowserButtonHook,
    registerResetAllowedLibrariesButtonHook,
    registerResetListenBrainzApiUrlButtonHook,
    registerResetMusicBrainzApiUrlButtonHook,
} from "./button";

export default function registerEventHooks(view: HTMLElement) {
    registerUserDropdownChangeHook(view);
    registerApiTokenCheckButtonHook(view);

    registerResetListenBrainzApiUrlButtonHook(view);
    registerResetMusicBrainzApiUrlButtonHook(view);
    registerBackupPathBrowserButtonHook(view);
    registerResetAllowedLibrariesButtonHook(view);

    registerUserConfigSubmitHook(view);
    registerGeneralConfigSubmitHook(view);
    registerMusicBrainzConfigSubmitHook(view);
    registerBackupConfigSubmitHook(view);
    registerLibrariesConfigSubmitHook(view);
}
