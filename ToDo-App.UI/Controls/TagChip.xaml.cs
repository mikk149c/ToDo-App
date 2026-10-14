using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace ToDo_App.UI.Controls
{
    /// <summary>
    /// A tag in the note dialogs: one pill split by a divider into two buttons. Clicking the name raises
    /// <see cref="EditRequested"/>, clicking the square ✕ area raises <see cref="RemoveRequested"/>.
    /// A tag that isn't saved yet has a dashed outline and a "+" icon.
    /// </summary>
    public sealed partial class TagChip : UserControl
    {
        public event EventHandler? EditRequested;
        public event EventHandler? RemoveRequested;

        public TagChip()
        {
            InitializeComponent();
        }

        /// <summary>Shows a tag's name and whether it's new, with the texts for tooltips and screen readers.</summary>
        public void Show(string name, bool isNew, string editLabel, string removeLabel)
        {
            NameText.Text = name;
            NewIcon.Visibility = isNew ? Visibility.Visible : Visibility.Collapsed;
            if (isNew)
            {
                Outline.StrokeDashArray = new DoubleCollection { 3, 2 };
            }
            else
            {
                Outline.ClearValue(Shape.StrokeDashArrayProperty);
            }

            AutomationProperties.SetName(EditButton, editLabel);
            ToolTipService.SetToolTip(EditButton, editLabel);
            AutomationProperties.SetName(RemoveButton, removeLabel);
            ToolTipService.SetToolTip(RemoveButton, removeLabel);
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
