using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using ToDo_App.Interfaces;
using ToDo_App.UI.Appearance;
using ToDo_App.UI.Board;
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
            public string? Color;
            public TagChip Chip = new TagChip();
        }

        private readonly List<TagEntry> entries = new List<TagEntry>();
        private ITodoBoard? board;
        private BoardChanges? changes;
        private TagEntry? flyoutEntry;
        private NoteTag? mergeTarget;
        private bool isSyncingColor;

        private Dictionary<string, string> localizationStrings = new Dictionary<string, string>();

        public TagEditor()
        {
            InitializeComponent();

            // The same swatches as the accent colour, so tag text stays readable (WCAG AA)
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

                ColorGrid.Items.Add(new GridViewItem { Content = swatch, Tag = option });
            }
        }

        /// <param name="board">Gives the existing tags, and renames or deletes a tag on every todo.</param>
        /// <param name="noteTags">The todo's current tags.</param>
        /// <param name="localizationStrings">Dictionary with relevant localized strings</param>
        public TagEditor(ITodoBoard board, BoardChanges changes, IEnumerable<NoteTag> noteTags, Dictionary<string,string> localizationStrings) : this()
        {
            this.board = board;
            this.changes = changes;
            SetLocalizationStrings(localizationStrings);

            foreach (NoteTag tag in noteTags)
            {
                addEntry(tag.Id, tag.Name, tag.Color);
            }
        }

        public void SetLocalizationStrings(Dictionary<string,string> localizationStrings)
        {
            this.localizationStrings = localizationStrings;
            TagInput.Header = localizationStrings["todo/property_tags"];
            TagInput.PlaceholderText = localizationStrings["dialog/add_tag_placeholder"];
            FlyoutNameBox.Header = localizationStrings["dialog/tag_name"];
            ColorHeader.Text = localizationStrings["dialog/tag_colour"];
            AutomationProperties.SetName(ColorGrid, localizationStrings["dialog/tag_colour"]);
            RenameButton.Content = localizationStrings["dialog/rename"];
            DeleteButton.Content = localizationStrings["dialog/delete"];
            ConfirmDeleteButton.Content = localizationStrings["dialog/delete"];
            CancelDeleteButton.Content = localizationStrings["dialog/cancel"];
            ConfirmMergeButton.Content = localizationStrings["dialog/merge"];
            CancelMergeButton.Content = localizationStrings["dialog/cancel"];
        }

        public IReadOnlyList<string> TagNames => entries.Select(entry => entry.Name).ToList();

        /// <summary>The colours chosen for tags that aren't on the board yet, by tag name.</summary>
        public IReadOnlyDictionary<string, string> NewTagColors => entries
            .Where(entry => entry.Id == null && entry.Color != null)
            .ToDictionary(entry => entry.Name, entry => entry.Color!, StringComparer.OrdinalIgnoreCase);

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
            TagEntry? entry = findEntry(name);
            if (entry == null)
            {
                NoteTag? existing = findBoardTag(name);
                entry = addEntry(existing?.Id, existing?.Name ?? name, existing?.Color);
            }

            TagInput.Text = "";
            TagInput.IsSuggestionListOpen = false;
            ErrorText.Visibility = Visibility.Collapsed;

            // Move focus off the box so the suggestions close and don't cover the tags, and onto the
            // added tag as feedback. Queued, because a chip that was just added isn't loaded yet
            DispatcherQueue.TryEnqueue(entry.Chip.FocusName);
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

        private TagEntry addEntry(Guid? id, string name, string? color)
        {
            var entry = new TagEntry { Id = id, Name = name, Color = color };
            entry.Chip.EditRequested += (sender, e) => openFlyout(entry);
            entry.Chip.RemoveRequested += (sender, e) => removeEntry(entry);
            showEntry(entry);

            entries.Add(entry);
            TagChips.Children.Add(entry.Chip);
            return entry;
        }

        private void showEntry(TagEntry entry)
        {
            bool isNew = entry.Id == null;
            string editLabel = string.Format(localizationStrings[isNew ? "dialog/new_tag_tooltip" : "dialog/edit_tag"], entry.Name);
            string removeLabel = string.Format(localizationStrings["dialog/remove_tag"], entry.Name);
            entry.Chip.Show(entry.Name, isNew, entry.Color, editLabel, removeLabel);
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
            MergeConfirmPanel.Visibility = Visibility.Collapsed;

            // A tag that isn't saved yet only exists in this dialog, so it can be renamed and given a colour
            // but not deleted
            DeleteButton.Visibility = entry.Id == null ? Visibility.Collapsed : Visibility.Visible;
            syncColorSelection(entry);
            FlyoutInfoText.Text = entry.Id is Guid tagId
                ? Localizer.HandlePlurals(localizationStrings["dialog/tag_used_on"],
                    board?.CountNotesWithTag(tagId) ?? 0, localizationStrings["lang_code"])
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

            // Renaming a tag on the board to another board tag's name offers to merge the two instead (#68)
            if (flyoutEntry.Id is Guid sourceId && findBoardTag(name, exceptId: sourceId) is NoteTag mergeTarget)
            {
                askToMerge(flyoutEntry, mergeTarget, sourceId);
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
                changes?.Send(board => board.UpdateTagName(tagId, name), tagId);
                flyoutEntry.Name = name;
            }
            else
            {
                // Only in this dialog. A name that matches a tag on the board becomes that tag
                NoteTag? existing = findBoardTag(name);
                flyoutEntry.Id = existing?.Id;
                flyoutEntry.Name = existing?.Name ?? name;
                flyoutEntry.Color = existing?.Color;
            }

            showEntry(flyoutEntry);
            TagFlyout.Hide();
        }

        private void OnColorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isSyncingColor || flyoutEntry == null
                || ColorGrid.SelectedItem is not GridViewItem { Tag: AccentColorOption option })
            {
                return;
            }

            if (flyoutEntry.Id is Guid tagId)
            {
                // On the board: changes the colour on every todo straight away
                string color = option.Hex;
                changes?.Send(board => board.UpdateTagColor(tagId, color), tagId);
            }
            // Otherwise it's only in this dialog, and the colour is given to the tag when the todo is saved
            flyoutEntry.Color = option.Hex;
            showEntry(flyoutEntry);
            TagFlyout.Hide();
        }

        private void syncColorSelection(TagEntry entry)
        {
            isSyncingColor = true;

            AccentColorOption current = AccentPalette.Find(entry.Color);
            ColorGrid.SelectedItem = ColorGrid.Items
                .Cast<GridViewItem>()
                .FirstOrDefault(item => ReferenceEquals(item.Tag, current));

            isSyncingColor = false;
        }

        private void OnDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (flyoutEntry?.Id is not Guid tagId)
            {
                return;
            }

            var textWithNumber = Localizer.HandlePlurals(localizationStrings["dialog/delete_tag_confirm"],
                board?.CountNotesWithTag(tagId) ?? 0, localizationStrings["lang_code"], 1);
            DeleteConfirmText.Text = string.Format(textWithNumber, flyoutEntry.Name);
            FlyoutActions.Visibility = Visibility.Collapsed;
            ColorPanel.Visibility = Visibility.Collapsed;
            DeleteConfirmPanel.Visibility = Visibility.Visible;
        }

        private void OnConfirmDeleteClicked(object sender, RoutedEventArgs e)
        {
            if (flyoutEntry?.Id is not Guid tagId)
            {
                return;
            }

            // Removes it from every todo straight away, including this one
            changes?.Send(board => board.DeleteTag(tagId), tagId);
            removeEntry(flyoutEntry);
            TagFlyout.Hide();
        }

        private void OnCancelDeleteClicked(object sender, RoutedEventArgs e)
        {
            DeleteConfirmPanel.Visibility = Visibility.Collapsed;
            FlyoutActions.Visibility = Visibility.Visible;
            ColorPanel.Visibility = Visibility.Visible;
        }

        private void askToMerge(TagEntry source, NoteTag target, Guid sourceId)
        {
            mergeTarget = target;
            var confirmTextWithNumber = Localizer.HandlePlurals(
                localizationStrings["dialog/merge_tag_confirm"],
                board?.CountNotesWithTag(sourceId) ?? 0,
                localizationStrings["lang_code"],
                2);
            MergeConfirmText.Text = string.Format(confirmTextWithNumber,
                source.Name, target.Name);
            FlyoutErrorText.Visibility = Visibility.Collapsed;
            FlyoutActions.Visibility = Visibility.Collapsed;
            ColorPanel.Visibility = Visibility.Collapsed;
            MergeConfirmPanel.Visibility = Visibility.Visible;
        }

        private void OnConfirmMergeClicked(object sender, RoutedEventArgs e)
        {
            if (flyoutEntry?.Id is not Guid sourceId || mergeTarget == null)
            {
                return;
            }

            // Every todo with the tag gets the target instead, straight away, including this one
            Guid targetId = mergeTarget.Id;
            changes?.Send(board => board.MergeTags(sourceId, targetId), sourceId, targetId);

            if (entries.Exists(entry => entry != flyoutEntry && entry.Id == mergeTarget.Id))
            {
                // This todo had both tags, so it keeps the target once
                removeEntry(flyoutEntry);
            }
            else
            {
                flyoutEntry.Id = mergeTarget.Id;
                flyoutEntry.Name = mergeTarget.Name;
                flyoutEntry.Color = mergeTarget.Color;
                showEntry(flyoutEntry);
            }
            TagFlyout.Hide();
        }

        private void OnCancelMergeClicked(object sender, RoutedEventArgs e)
        {
            MergeConfirmPanel.Visibility = Visibility.Collapsed;
            FlyoutActions.Visibility = Visibility.Visible;
            ColorPanel.Visibility = Visibility.Visible;
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
