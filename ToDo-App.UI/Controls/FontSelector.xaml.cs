using Microsoft.Graphics.Canvas.Text;
using ToDo_App.UI.Appearance;

namespace ToDo_App.UI.Controls
{
    public sealed partial class FontSelector : UserControl
    {
        private readonly AppearanceManager appearance = ((App)Application.Current).Appearance;
        private bool isSyncing;
        public Action<string?> OnFontSelected;

        private const string DefaultFontOption = "_DEFAULT_FONT_";

        public FontSelector()
        {
            InitializeComponent();
            FontComboBox.Items.Add(new ComboBoxItem
            {
                Content = new StackPanel
                {
                    Children = {
                        new SymbolIcon(Symbol.More)
                    },
                    Orientation = Orientation.Horizontal
                },
                Tag = DefaultFontOption
            });
            foreach (var font in CanvasTextFormat.GetSystemFontFamilies().OrderBy(f => f))
            {
                FontComboBox.Items.Add(new ComboBoxItem
                {
                    Content = font,
                    Tag = font
                });
            }
            
            sync();
        }

        private void OnFontSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSyncing || FontComboBox.SelectedItem is not ComboBoxItem { Tag: string font })
            {
                return;
            }

            if (font == DefaultFontOption)
            {
                appearance.SetFont(null);
                OnFontSelected.Invoke(null);
                return;
            }
            
            OnFontSelected.Invoke(font);
            appearance.SetFont(font);
        }

        private void sync()
        {
            isSyncing = true;

            var font = appearance.Current.Font;

            if (font == null)
                font = DefaultFontOption;
            
            foreach (ComboBoxItem item in FontComboBox.Items)
            {
                if ((string)item.Tag == font)
                {
                    FontComboBox.SelectedItem = item;
                }
            }

            isSyncing = false;
        }
    }
}
