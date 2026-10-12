using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace ToDo_App.UI.Appearance
{
    /// <summary>
    /// Owns the current appearance, applies it to the running app and persists changes.
    /// </summary>
    public sealed class AppearanceManager
    {
        // How far the accent is blended towards white for text shown on dark backgrounds
        private const double DarkThemeTextLightening = 0.4;

        private readonly AppearanceSettingsStore store;
        private FrameworkElement? root;

        public AppearanceManager(AppearanceSettingsStore store)
        {
            this.store = store;
            Current = store.Load();
        }

        public AppearanceSettings Current { get; private set; }

        public event EventHandler? Changed;

        /// <summary>Applies the stored appearance to <paramref name="rootElement"/> and the app resources.</summary>
        public void Attach(FrameworkElement rootElement)
        {
            root = rootElement;
            apply();
        }

        public void SetTheme(ThemePreference theme)
        {
            if (Current.Theme == theme)
            {
                return;
            }

            update(Current with { Theme = theme });
        }

        public void SetAccent(AccentColorOption accent)
        {
            if (string.Equals(Current.AccentColor, accent.Hex, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            update(Current with { AccentColor = accent.Hex });
        }

        private void update(AppearanceSettings settings)
        {
            Current = settings;
            apply();
            store.Save(settings);
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void apply()
        {
            if (root != null)
            {
                root.RequestedTheme = Current.Theme switch
                {
                    ThemePreference.Light => ElementTheme.Light,
                    ThemePreference.Dark => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }

            Color accent = AccentPalette.Find(Current.AccentColor).Color;
            ResourceDictionary resources = Application.Current.Resources;

            // Updating the brush in place restyles every control already using it
            ((SolidColorBrush)resources["PrimaryBrush"]).Color = accent;
            setAccentText("Light", accent);
            setAccentText("Dark", lighten(accent, DarkThemeTextLightening));
        }

        private static void setAccentText(string themeKey, Color color)
        {
            var dictionary = (ResourceDictionary)Application.Current.Resources.ThemeDictionaries[themeKey];
            ((SolidColorBrush)dictionary["AccentTextBrush"]).Color = color;
        }

        private static Color lighten(Color color, double amount)
        {
            byte blend(byte channel) => (byte)Math.Round(channel + (255 - channel) * amount);
            return Color.FromArgb(color.A, blend(color.R), blend(color.G), blend(color.B));
        }
    }
}
