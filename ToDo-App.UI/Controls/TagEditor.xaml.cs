using Microsoft.UI.Xaml.Automation;

namespace ToDo_App.UI.Controls
{
    /// <summary>
    /// Edits the tags of a todo before it's saved: type a name and press Enter to add it (tags already on
    /// the board are suggested), and click a tag to remove it. The name rules come from <see cref="NoteTag"/>.
    /// </summary>
    public sealed partial class TagEditor : UserControl
    {
        private readonly List<string> tagNames = new List<string>();
        private readonly List<string> existingTagNames = new List<string>();

        private Dictionary<string, string> localizationStrings;

        public TagEditor()
        {
            InitializeComponent();
        }

        /// <param name="tagNames">The todo's current tags.</param>
        /// <param name="existingTagNames">Every tag on the board, offered as suggestions.</param>
        /// <param name="localizationStrings">Dictionary with relevant localized strings</param>
        public TagEditor(IEnumerable<string> tagNames, IEnumerable<string> existingTagNames, Dictionary<string,string> localizationStrings) : this()
        {
            SetLocalizationStrings(localizationStrings);
            
            this.existingTagNames.AddRange(existingTagNames);
            foreach (string name in tagNames)
            {
                addChip(name);
            }
        }

        public void SetLocalizationStrings(Dictionary<string,string> localizationStrings)
        {
            this.localizationStrings = localizationStrings;
            TagInput.Header = localizationStrings["todo/property_tags"];
            TagInput.PlaceholderText = localizationStrings["dialog/add_tag_placeholder"];
        }

        public IReadOnlyList<string> TagNames => tagNames;

        /// <summary>
        /// Adds a name that's still typed in the box but wasn't submitted with Enter.
        /// Returns false, and shows why, if that name isn't valid.
        /// </summary>
        public bool CommitPendingInput()
        {
            return string.IsNullOrWhiteSpace(TagInput.Text) || tryAddTag(TagInput.Text);
        }

        private void OnTagSubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            tryAddTag(args.ChosenSuggestion as string ?? args.QueryText);
        }

        private void OnTagInputTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                ErrorText.Visibility = Visibility.Collapsed;
                updateSuggestions();
            }
        }

        private void OnTagInputGotFocus(object sender, RoutedEventArgs e)
        {
            updateSuggestions();
        }

        private bool tryAddTag(string text)
        {
            string name;
            try
            {
                name = NoteTag.NormalizeName(text);
            }
            catch (ArgumentException e)
            {
                ErrorText.Text = e.Message;
                ErrorText.Visibility = Visibility.Visible;
                return false;
            }

            // A todo can't have the same tag twice, and an existing tag keeps its spelling
            if (!tagNames.Exists(existing => NoteTag.NamesMatch(existing, name)))
            {
                addChip(existingTagNames.Find(existing => NoteTag.NamesMatch(existing, name)) ?? name);
            }

            TagInput.Text = "";
            TagInput.IsSuggestionListOpen = false;
            ErrorText.Visibility = Visibility.Collapsed;
            return true;
        }

        private void updateSuggestions()
        {
            string text = TagInput.Text.Trim();
            List<string> suggestions = existingTagNames
                .Where(name => !tagNames.Exists(added => NoteTag.NamesMatch(added, name)))
                .Where(name => name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToList();

            TagInput.ItemsSource = suggestions;
            TagInput.IsSuggestionListOpen = suggestions.Count > 0;
        }

        private void addChip(string name)
        {
            tagNames.Add(name);

            var chip = new Button
            {
                Padding = new Thickness(8, 2, 6, 2),
                CornerRadius = new CornerRadius(10),
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        new TextBlock { Text = name },
                        new FontIcon { Glyph = "", FontSize = 10 }
                    }
                }
            };
            AutomationProperties.SetName(chip, $"Remove tag {name}");
            ToolTipService.SetToolTip(chip, string.Format(localizationStrings["dialog/remove_tag"], name));
            chip.Click += (sender, e) =>
            {
                tagNames.Remove(name);
                TagChips.Children.Remove(chip);
            };

            TagChips.Children.Add(chip);
        }
    }
}
