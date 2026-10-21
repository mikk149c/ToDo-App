using System.Globalization;
using Microsoft.JSInterop;

namespace ToDo_App.Web.Appearance;

/// <summary>
/// Owns the current appearance for one browser tab, applies it to the page and remembers changes.
/// </summary>
public sealed class AppearanceManager
{
    // How far the accent is blended towards white for text shown on dark backgrounds
    private const double DarkThemeTextLightening = 0.4;

    private readonly BrowserPreferences store;
    private readonly IJSRuntime js;

    public AppearanceManager(BrowserPreferences store, IJSRuntime js)
    {
        this.store = store;
        this.js = js;
    }

    // Klingon is shown in Georgia unless another font is chosen, as in the Windows app
    private const string KlingonDefaultFont = "Georgia";

    private string language = "en-US";

    public AppearanceSettings Current { get; private set; } = new AppearanceSettings();

    public event EventHandler? Changed;

    /// <summary>Loads the remembered appearance and applies it to the page.</summary>
    public async Task AttachAsync()
    {
        Current = await store.LoadAppearanceAsync();
        await applyAsync();
    }

    public Task SetThemeAsync(ThemePreference theme)
    {
        return Current.Theme == theme ? Task.CompletedTask : updateAsync(Current with { Theme = theme });
    }

    public Task SetAccentAsync(AccentColorOption accent)
    {
        return string.Equals(Current.AccentColor, accent.Hex, StringComparison.OrdinalIgnoreCase)
            ? Task.CompletedTask
            : updateAsync(Current with { AccentColor = accent.Hex });
    }

    public Task SetFontAsync(string? font)
    {
        font = string.IsNullOrWhiteSpace(font) ? null : font;
        return Current.Font == font ? Task.CompletedTask : updateAsync(Current with { Font = font });
    }

    /// <summary>Tells the manager which language is shown, as that can change the default font.</summary>
    public async Task SetLanguageAsync(string languageCode)
    {
        language = languageCode;
        await applyAsync();
    }

    private async Task updateAsync(AppearanceSettings settings)
    {
        Current = settings;
        await applyAsync();
        await store.SaveAppearanceAsync(settings);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private async Task applyAsync()
    {
        string theme = Current.Theme switch
        {
            ThemePreference.Light => "light",
            ThemePreference.Dark => "dark",
            _ => "system"
        };
        string accent = AccentPalette.Find(Current.AccentColor).Hex;

        try
        {
            string? font = Current.Font ?? (language == "kln" ? KlingonDefaultFont : null);
            await js.InvokeVoidAsync("todoApp.appearance.apply", theme, accent, Lighten(accent, DarkThemeTextLightening), font);
        }
        catch (JSException)
        {
            // The page keeps its current look. Nothing else depends on it
        }
    }

    /// <summary>Blends a "#RRGGBB" colour towards white by <paramref name="amount"/> (0 to 1).</summary>
    public static string Lighten(string hex, double amount)
    {
        string blend(int start)
        {
            int channel = int.Parse(hex.Substring(start, 2), NumberStyles.HexNumber);
            return ((int)Math.Round(channel + (255 - channel) * amount)).ToString("X2");
        }
        return $"#{blend(1)}{blend(3)}{blend(5)}";
    }
}
