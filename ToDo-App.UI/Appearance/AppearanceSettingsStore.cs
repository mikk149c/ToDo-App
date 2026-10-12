using System.Diagnostics;
using System.Text.Json;

namespace ToDo_App.UI.Appearance
{
    /// <summary>
    /// Persists <see cref="AppearanceSettings"/> as a small JSON file in the user's local
    /// app data folder. Failing to read or write must never stop the app, so problems fall
    /// back to defaults instead of throwing.
    /// </summary>
    public sealed class AppearanceSettingsStore
    {
        private readonly string path;

        public AppearanceSettingsStore()
            : this(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ToDo-App",
                "ui-settings.json"))
        {
        }

        public AppearanceSettingsStore(string path)
        {
            this.path = path;
        }

        public AppearanceSettings Load()
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new AppearanceSettings();
                }

                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize(json, AppearanceJsonContext.Default.AppearanceSettings)
                       ?? new AppearanceSettings();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
            {
                Debug.WriteLine($"Could not load appearance settings: {e.Message}");
                return new AppearanceSettings();
            }
        }

        public void Save(AppearanceSettings settings)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(settings, AppearanceJsonContext.Default.AppearanceSettings));
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                Debug.WriteLine($"Could not save appearance settings: {e.Message}");
            }
        }
    }
}
