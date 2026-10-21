// Small browser helpers called from C# through IJSRuntime. Everything else is in the Razor components.
window.todoApp = {
    // UI preferences (theme, accent colour, language), remembered per browser. See BrowserPreferences.cs
    preferences: {
        get(key) {
            try { return localStorage.getItem("todo-app." + key); } catch { return null; }
        },
        set(key, value) {
            try { localStorage.setItem("todo-app." + key, value); } catch { /* private window or storage blocked */ }
        }
    },

    // See AppearanceManager.cs. theme is "light", "dark" or "system"
    appearance: {
        apply(theme, accent, accentTextDark, font) {
            const root = document.documentElement;
            root.dataset.theme = theme;
            root.style.setProperty("--accent", accent);
            root.style.setProperty("--accent-text-light", accent);
            if (accentTextDark) {
                root.style.setProperty("--accent-text-dark", accentTextDark);
            }
            // The chosen font, or null to use the app's own (the fallback in app.css)
            if (font) {
                root.style.setProperty("--app-font", `"${font.replace(/["\\]/g, "")}", system-ui, sans-serif`);
            } else {
                root.style.removeProperty("--app-font");
            }
        },

        // Runs before the page is drawn. The accent text for dark backgrounds follows once C# applies the appearance
        applyStored() {
            const theme = { Light: "light", Dark: "dark" }[todoApp.preferences.get("theme")] ?? "system";
            const accent = todoApp.preferences.get("accent");
            todoApp.appearance.apply(theme, /^#[0-9A-Fa-f]{6}$/.test(accent ?? "") ? accent : "#512BD4", null,
                todoApp.preferences.get("font"));
        }
    },

    // A native modal <dialog>: focus stays inside and the rest of the page can't be clicked.
    // Escape is handled in C# (ContentDialog.razor), so the browser's own close is turned off
    dialog: {
        show(element) {
            if (!element.open) {
                element.addEventListener("cancel", event => event.preventDefault());
                element.showModal();
            }
        },
        close(element) {
            if (element?.open) {
                element.close();
            }
        }
    },

    // Places a position: fixed flyout below its anchor, or above it when there's no room below,
    // so a dialog's edge never cuts it off. Like Placement="Bottom" on a WinUI flyout
    placeFlyout(flyout, anchor) {
        const margin = 8;
        const a = anchor.getBoundingClientRect();
        const f = flyout.getBoundingClientRect();
        let top = a.bottom + 4;
        if (top + f.height > window.innerHeight - margin) {
            top = Math.max(margin, a.top - 4 - f.height);
        }
        const left = Math.max(margin, Math.min(a.left, window.innerWidth - f.width - margin));
        flyout.style.top = `${top}px`;
        flyout.style.left = `${left}px`;
        flyout.style.visibility = "visible";
    }
};

// Firefox only starts dragging an element when some data is set
document.addEventListener("dragstart", event => {
    if (event.dataTransfer && event.target instanceof HTMLElement && event.target.draggable) {
        event.dataTransfer.setData("text/plain", "");
        event.dataTransfer.effectAllowed = "move";
    }
});

todoApp.appearance.applyStored();
