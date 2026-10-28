namespace ToDo_App.Web.Appearance;

public enum ThemePreference
{
    SystemDefault,
    Light,
    Dark
}

/// <summary>
/// How the user wants the app to look. This is purely a UI concern and is never passed to the backend.
/// </summary>
public sealed record AppearanceSettings
{
    public ThemePreference Theme { get; init; } = ThemePreference.SystemDefault;

    /// <summary>Accent colour as #RRGGBB. Unknown values fall back to the default accent.</summary>
    public string AccentColor { get; init; } = AccentPalette.Default.Hex;

    /// <summary>The font the user chose, or null for the app's own font.</summary>
    public string? Font { get; init; }
}
