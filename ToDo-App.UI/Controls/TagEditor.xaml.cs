using Microsoft.UI.Xaml.Input;
using ToDo_App.Interfaces;
using Windows.System;

namespace ToDo_App.UI.Controls
{
    /// <summary>
    /// Edits the tags of a todo before it's saved: type a name and press Enter to add it (tags already on
    /// the board are suggested), click a tag's ✕ to remove it from the todo, and click its name to rename
    /// it, or delete it from every todo. The name rules come from <see cref="NoteTag"/>.
    /// </summary>
    /// <remarks>
    /// Adding and removing only apply when the todo is saved. Renaming or deleting a tag that's on the board
    /// changes every todo with it, so that applies straight away (#65).
    /// </remarks>
    public sealed partial class TagEditor : UserControl
    {
        // A tag shown in the editor. Id is null while the tag isn't on the board yet
        private sealed class TagEntry
        {
            public Guid? Id;
            public string Name = "";
            public TagChip Chip = new TagChip();
        }

        private readonly List<TagEntry> entries = new List<TagEntry>();
        private ITodoBoard? board;
        private TagEntry? flyoutEntry;

        private Dictionary<string, string> localizationStrings = new Dictionary<string, string>();

        public TagEditor()
        {
            InitializeComponent();
        }

        /// <param name="board">Gives the existing tags, and renames or deletes a tag on every todo.</param>
        /// <param name="noteTags">The todo's current tags.</param>
        /// <param name="localizationStrings">Dictionary with relevant localized strings</param>
        public TagEditor(ITodoBoard board, IEnumerable<NoteTag> noteTags, Dictionary<string,string> localizationStrings) : this()
        {
            this.board = board;
            SetLocalizationStrings(localizationStrings);

            foreach (NoteTag tag in noteTags)
            {
                addEntry(tag.Id, tag.Name);
            }
        }

        public void SetLocalizationStrings(Dictionary<string,string> localizationStrings)
        {
            this.localizationStrings = localizationStrings;
            TagInput.Header = localizationStrings["todo/property_tags"];
            TagInput.PlaceholderText = localizationStrings["dialog/add_tag_placeholder"];
            FlyoutNameBox.Header = localizationStrings["dialog/tag_name"];
            RenameButton.Content = localizationStrings["dialog/rename"];
            DeleteButton.Content = localizationStrings["dialog/delete"];
            ConfirmDeleteButton.Content = localizationStrings["dialog/delete"];
            CancelDeleteButton.Content = localizationStrings["dialog/cancel"];
        }

        public IReadOnlyList<string> TagNames => entries.Select(entry => entry.Name).ToList();

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
            if (!tryNormalize(text, out string name, out string error))
            {
                ErrorText.Text = error;
                ErrorText.Visibility = Visibility.Visible;
                return false;
            }

            // A todo can't have the same tag twice, and a tag already on the board keeps its Id and spelling
            if (findEntry(name) == null)
            {
                NoteTag? existing = findBoardTag(name);
                addEntry(existing?.Id, existing?.Name ?? name);
            }

            TagInput.Text = "";
            TagInput.IsSuggestionListOpen = false;
            ErrorText.Visibility = Visibility.Collapsed;
            return true;
        }

        private void updateSuggestions()
        {
            string text = TagInput.Text.Trim();
            List<string> suggestions = boardTags()
                .Select(tag => tag.Name)
                .Where(name => findEntry(name) == null)
                .Where(name => name.Contains(text, StringComparison.OrdinalIgnoreCase))
                .ToList();

            TagInput.ItemsSource = suggestions;
            TagInput.IsSuggestionListOpen = suggestions.Count > 0;
        }

        private void addEntry(Guid? id, string name)
        {
            var entry = new TagEntry { Id = id, Name = name };
            entry.Chip.EditRequested += (sender, e) => openFlyout(entry);
            entry.Chip.RemoveRequested += (sender, e) => removeEntry(entry);
            showEntry(entry);

            entries.Add(entry);
            TagChips.Children.Add(entry.Chip);
        }

        private void showEntry(TagEntry entry)
        {
            bool isNew = entry.Id == null;
            string editLabel = string.Format(localizationStrings[isNew ? "dialog/new_tag_tooltip" : "dialog/edit_tag"], entry.Name);
            string removeLabel = string.Format(localizationStrings["dialog/remove_tag"], entry.Name);
            entry.Chip.Show(entry.Name, isNew, editLabel, removeLabel);
        }

        private void removeEntry(TagEntry entry)
        {
            entries.Remove(entry);
            TagChips.Children.Remove(entry.Chip);
        }

        // --- Flyout for renaming or deleting a tag ---

        private void openFlyout(TagEntry entry)
        {
            flyoutEntry = entry;
            FlyoutNameBox.Text = entry.Name;
            FlyoutErrorText.Visibility = Visibility.Collapsed;
            FlyoutActions.Visibility = Visibility.Visible;
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;

            // A tag that isn't saved yet only exists in this dialog, so it can be renamed but not deleted
            DeleteButton.Visibility = entry.Id == null ? Visibility.Collapsed : Visibility.Visible;
            FlyoutInfoText.Text = entry.Id is Guid tagId
                ? string.Format(localizationStrings["dialog/tag_used_on"], board?.CountNotesWithTag(tagId) ?? 0)
                : localizationStrings["dialog/tag_new_info"];

            // Flyouts open outside the dialog's visual tree, so match the dialog's theme
            FlyoutRoot.RequestedTheme = ActualTheme;
            TagFlyout.ShowAt(entry.Chip);
        }

        private void OnFlyoutNameKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                renameFlyoutEntry();
            }
        }

        private void OnRenameClicked(object sender, RoutedEventArgs e)
        {
            renameFlyoutEntry();
        }

        private void renameFlyoutEntry()
        {
            if (flyoutEntry == null)
            {
                return;
            }

            if (!tryNormalize(FlyoutNameBox.Text, out string name, out string error))
            {
                showFlyoutError(error);
                return;
            }

            // Two different tags can't share a name, whether they're on the board or only in this dialog
            bool nameTaken = findEntry(name, except: flyoutEntry) != null
                             || (flyoutEntry.Id is Guid id && findBoardTag(name, exceptId: id) != null);
            if (nameTaken)
            {
                showFlyoutError(string.Format(localizationStrings["dialog/tag_name_taken"], name));
                return;
            }

            if (flyoutEntry.Id is Guid tagId)
            {
                // On the board: renames it on every todo straight away
                board?.RenameTag(tagId, name);
                flyoutEntry.Name = name;
            }
            else
            {
                // Only in this dialog. A name that matches a tag on the board becomes that tag
                NoteTag? existing = findBoardTag(name);
                flyoutEntry.Id = existing?.Id;
                flyoutEntry.Name = existing?.Name ?? name;
            }

            showEntry(flyoutEntry);
            TagFlyout.Hide();
        }

        private void OnDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (flyoutEntry?.Id is not Guid tagId)
            {
                return;
            }

            DeleteConfirmText.Text = string.Format(localizationStrings["dialog/delete_tag_confirm"],
                flyoutEntry.Name, board?.CountNotesWithTag(tagId) ?? 0);
            FlyoutActions.Visibility = Visibility.Collapsed;
            DeleteConfirmPanel.Visibility = Visibility.Visible;
        }

        private void OnConfirmDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (flyoutEntry?.Id is not Guid tagId)
            {
                return;
            }

            // Removes it from every todo straight away, including this one
            board?.DeleteTag(tagId);
            removeEntry(flyoutEntry);
            TagFlyout.Hide();
        }

        private void OnCancelDeleteClicked(object sender, RoutedEventArgs e)
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;
            FlyoutActions.Visibility = Visibility.Visible;
        }

        private void showFlyoutError(string message)
        {
            FlyoutErrorText.Text = message;
            FlyoutErrorText.Visibility = Visibility.Visible;
        }

        // --- Lookups ---

        private List<NoteTag> boardTags()
        {
            return board?.GetTags() ?? new List<NoteTag>();
        }

        private TagEntry? findEntry(string name, TagEntry? except = null)
        {
            return entries.Find(entry => entry != except && NoteTag.NamesMatch(entry.Name, name));
        }

        private NoteTag? findBoardTag(string name, Guid? exceptId = null)
        {
            return boardTags().Find(tag => tag.Id != exceptId && NoteTag.NamesMatch(tag.Name, name));
        }

        private static bool tryNormalize(string text, out string name, out string error)
        {
            try
            {
                name = NoteTag.NormalizeName(text);
                error = "";
                return true;
            }
            catch (ArgumentException e)
            {
                name = "";
                error = e.Message;
                return false;
            }
        }
    }
}
