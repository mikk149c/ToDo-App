using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using ToDo_App.UI.Appearance;

namespace ToDo_App.UI.Controls
{
    /// <summary>
    /// A tag in the note dialogs: one pill split by a divider into two buttons. Clicking the name raises
    /// <see cref="EditRequested"/>, clicking the square ✕ area raises <see cref="RemoveRequested"/>.
    /// A tag that isn't saved yet has a dashed accent outline and an accent "+" icon.
    /// </summary>
    public sealed partial class TagChip : UserControl
    {
        public event EventHandler? EditRequested;
        public event EventHandler? RemoveRequested;

        public TagChip()
        {
            InitializeComponent();
        }

        /// <summary>Shows a tag's name, colour and whether it's new, with the texts for tooltips and screen readers.</summary>
        public void Show(string name, bool isNew, string? color, string editLabel, string removeLabel)
        {
            NameText.Text = name;
            ColorDot.Fill = new SolidColorBrush(AccentPalette.Find(color).Color);
            VisualStateManager.GoToState(this, isNew ? "New" : "Saved", false);

            AutomationProperties.SetName(EditButton, editLabel);
            ToolTipService.SetToolTip(EditButton, editLabel);
            AutomationProperties.SetName(RemoveButton, removeLabel);
            ToolTipService.SetToolTip(RemoveButton, removeLabel);
        }

        /// <summary>Moves focus to the tag's name, e.g. to show which tag was just added.</summary>
        public void FocusName()
        {
            EditButton.Focus(FocusState.Programmatic);
        }

        private void OnEditClicked(object sender, RoutedEventArgs e)
        {
            EditRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnRemoveClicked(object sender, RoutedEventArgs e)
        {
            RemoveRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
