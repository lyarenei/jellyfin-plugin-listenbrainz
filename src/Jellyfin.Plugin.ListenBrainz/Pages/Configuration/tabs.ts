export function initTabs(view: HTMLElement): void {
    const tabButtons = view.querySelectorAll<HTMLButtonElement>(".tab-button");
    tabButtons.forEach((button) => {
        button.addEventListener("click", () => {
            const tabId = button.dataset.tab;
            if (tabId) {
                switchTab(view, tabId);
            }
        });
    });
}

function switchTab(view: HTMLElement, tabId: string): void {
    const allButtons = view.querySelectorAll<HTMLButtonElement>(".tab-button");
    allButtons.forEach((btn) => btn.classList.remove("active"));

    const allPanels = view.querySelectorAll<HTMLElement>(".tab-content");
    allPanels.forEach((panel) => panel.classList.remove("active"));

    const activeButton = view.querySelector<HTMLButtonElement>(`.tab-button[data-tab="${tabId}"]`);
    activeButton?.classList.add("active");

    const activePanel = view.querySelector<HTMLElement>(`#tab-${tabId}`);
    activePanel?.classList.add("active");
}
