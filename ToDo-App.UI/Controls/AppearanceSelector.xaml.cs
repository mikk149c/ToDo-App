using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using ToDo_App.UI.Appearance;

namespace ToDo_App.UI.Controls
{
    public sealed partial class AppearanceSelector : UserControl
    {
        private readonly AppearanceManager appearance = ((App)Application.Current).Appearance;
        private bool isSyncing;

        public AppearanceSelector()
        {
            InitializeComponent();

            foreach (AccentColorOption option in AccentPalette.All)
            {
                var swatch = new Border
                {
                    Width = 32,
                    Height = 32,
                    CornerRadius = new CornerRadius(4),
                    Background = new SolidColorBrush(option.Color)
                };
                ToolTipService.SetToolTip(swatch, option.Name);
                AutomationProperties.SetName(swatch, option.Name);

                AccentGrid.Items.Add(new GridViewItem { Content = swatch, Tag = option });
            }

            sync();
        }

        private void OnThemeClicked(object sender, RoutedEventArgs e)
        {
            var theme = Enum.Parse<ThemePreference>((string)((ToggleButton)sender).Tag);
            appearance.SetTheme(theme);

            // A toggle flips itself when clicked, so re-assert that exactly one is checked
            sync();
        }

        private void OnAccentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSyncing || AccentGrid.SelectedItem is not GridViewItem { Tag: AccentColorOption accent })
            {
                return;
            }

            appearance.SetAccent(accent);
            AccentFlyout.Hide();
        }

        private void sync()
        {
            isSyncing = true;

            ThemePreference theme = appearance.Current.Theme;
            LightButton.IsChecked = theme == ThemePreference.Light;
            SystemButton.IsChecked = theme == ThemePreference.SystemDefault;
            DarkButton.IsChecked = theme == ThemePreference.Dark;

            AccentColorOption current = AccentPalette.Find(appearance.Current.AccentColor);
            foreach (GridViewItem item in AccentGrid.Items)
            {
                if (ReferenceEquals(item.Tag, current))
                {
                    AccentGrid.SelectedItem = item;
                }
            }

            isSyncing = false;
        }
    }
}
