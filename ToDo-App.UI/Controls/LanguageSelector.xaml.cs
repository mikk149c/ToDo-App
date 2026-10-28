using ToDo_App.UI.Appearance;

namespace ToDo_App.UI.Controls
{
    public sealed partial class LanguageSelector : UserControl
    {
        private readonly AppearanceManager appearance = ((App)Application.Current).Appearance;
        private bool isSyncing;
        public Action<string> OnLanguageSelected;

        public LanguageSelector()
        {
            InitializeComponent();
            foreach (var language in Localizer.Languages)
            {
                LanguageComboBox.Items.Add(new ComboBoxItem
                {
                    Content = language.Value,
                    Tag = language.Key
                });
            }

            sync();
        }

        public void SetLocalizedStrings(string language)
        {
            ToolTipService.SetToolTip(LanguageComboBox, language);
        }

        public void SetSelectedLanguage(string language)
        {
            appearance.SetLanguage(language);
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if ((string)item.Tag == language)
                {
                    LanguageComboBox.SelectedItem = item;
                }
            }
        }

        private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSyncing || LanguageComboBox.SelectedItem is not ComboBoxItem { Tag: string language })
            {
                return;
            }
            OnLanguageSelected.Invoke(language);
            appearance.SetLanguage(language);
        }

        private void sync()
        {
            isSyncing = true;

            var language = appearance.Current.Language;
            
            foreach (ComboBoxItem item in LanguageComboBox.Items)
            {
                if ((string)item.Tag == language)
                {
                    LanguageComboBox.SelectedItem = item;
                }
            }
            
            isSyncing = false;
        }
    }
}
