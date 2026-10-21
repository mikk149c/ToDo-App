using System.Text.Json.Serialization;

namespace ToDo_App.UI.Appearance
{
    public enum ThemePreference
    {
        SystemDefault,
        Light,
        Dark
    }

    /// <summary>
    /// How the user wants the app to look. This is purely a UI concern and is never
    /// passed to the backend.
    /// </summary>
    public sealed record AppearanceSettings
    {
        public ThemePreference Theme { get; init; } = ThemePreference.SystemDefault;

        /// <summary>Accent colour as #RRGGBB. Unknown values fall back to the default accent.</summary>
        public string AccentColor { get; init; } = AccentPalette.Default.Hex;
        public string Language { get; init; } = "en-US";
        public string? Font { get; init; } = null;
    }

    [JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(AppearanceSettings))]
    internal sealed partial class AppearanceJsonContext : JsonSerializerContext
    {
    }
}
