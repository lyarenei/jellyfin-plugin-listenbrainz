import { registerUserDropdownChangeHook } from "./userDropdown";
import { registerApiTokenCheckButtonHook } from "./apiToken";
import { registerSubmitButtonHook } from "./submit";
import {
    registerBackupPathBrowserButtonHook,
    registerResetListenBrainzApiUrlButtonHook,
    registerResetMusicBrainzApiUrlButtonHook,
} from "./button";

export default function registerEventHooks(view: HTMLElement) {
    registerUserDropdownChangeHook(view);
    registerApiTokenCheckButtonHook(view);

    registerResetListenBrainzApiUrlButtonHook(view);
    registerResetMusicBrainzApiUrlButtonHook(view);
    registerBackupPathBrowserButtonHook(view);

    registerSubmitButtonHook(view);
}
