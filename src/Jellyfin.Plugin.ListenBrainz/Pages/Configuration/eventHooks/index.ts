import { registerUserDropdownChangeHook } from "./userDropdown";
import { registerApiTokenCheckButtonHook } from "./apiToken";
import { registerSubmitButtonHook } from "./submit";

export default function registerEventHooks(view: HTMLElement) {
    registerUserDropdownChangeHook(view);
    registerApiTokenCheckButtonHook(view);
    registerSubmitButtonHook(view);
}
