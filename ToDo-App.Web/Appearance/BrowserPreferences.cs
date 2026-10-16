using Microsoft.JSInterop;

namespace ToDo_App.Web.Appearance;

/// <summary>
/// Remembers UI preferences (theme, accent colour, language) in the browser's localStorage, the web
/// counterpart of the Windows app's settings file. Failing to read or write must never stop the app,
/// so problems fall back to defaults instead of throwing.
/// </summary>
public sealed class BrowserPreferences
{
    private readonly IJSRuntime js;
    private readonly ILogger<BrowserPreferences> logger;

    public BrowserPreferences(IJSRuntime js, ILogger<BrowserPreferences> logger)
    {
        this.js = js;
        this.logger = logger;
    }

    public async Task<AppearanceSettings> LoadAppearanceAsync()
    {
        string? theme = await getAsync("theme");
        string? accent = await getAsync("accent");
        return new AppearanceSettings
        {
            Theme = Enum.TryParse(theme, out ThemePreference parsed) ? parsed : ThemePreference.SystemDefault,
            AccentColor = AccentPalette.Find(accent).Hex
        };
    }

    public async Task SaveAppearanceAsync(AppearanceSettings settings)
    {
        await setAsync("theme", settings.Theme.ToString());
        await setAsync("accent", settings.AccentColor);
    }

    public Task<string?> LoadLanguageAsync() => getAsync("language");

    public Task SaveLanguageAsync(string language) => setAsync("language", language);

    private async Task<string?> getAsync(string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("todoApp.preferences.get", key);
        }
        catch (JSException e)
        {
            logger.LogDebug("Could not load the preference {Key}: {Message}", key, e.Message);
            return null;
        }
    }

    private async Task setAsync(string key, string value)
    {
        try
        {
            await js.InvokeVoidAsync("todoApp.preferences.set", key, value);
        }
        catch (JSException e)
        {
            logger.LogDebug("Could not save the preference {Key}: {Message}", key, e.Message);
        }
    }
}
