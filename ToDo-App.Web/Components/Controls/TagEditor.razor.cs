using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ToDo_App.Interfaces;
using ToDo_App.Web.Appearance;

namespace ToDo_App.Web.Components.Controls;

/// <summary>
/// Edits the tags of a todo before it's saved: type a name and press Enter to add it (tags already on
/// the board are suggested), click a tag's ✕ to remove it from the todo, and click its name to rename
/// it, change its colour, or delete it from every todo. The name rules come from <see cref="NoteTag"/>.
/// </summary>
/// <remarks>
/// Adding and removing only apply when the todo is saved. Renaming, recolouring or deleting a tag that's on
/// the board changes every todo with it, so that applies straight away (#65, #66).
/// The same behaviour as <c>ToDo-App.UI/Controls/TagEditor</c>.
/// </remarks>
public partial class TagEditor
{
    // A tag shown in the editor. Id is null while the tag isn't on the board yet
    private sealed class TagEntry
    {
        public Guid? Id;
        public string Name = "";
        public string? Color;
        public TagChip? Chip;
        public ElementReference Anchor;
    }

    private readonly string inputId = $"tag-input-{Guid.NewGuid():N}";
    private readonly string listId = $"tag-suggestions-{Guid.NewGuid():N}";
    private readonly string errorId = $"tag-error-{Guid.NewGuid():N}";
    private readonly string flyoutNameId = $"tag-name-{Guid.NewGuid():N}";
    private readonly string colorHeaderId = $"tag-colour-{Guid.NewGuid():N}";

    private readonly List<TagEntry> entries = new List<TagEntry>();
    private string inputText = "";
    private string? errorText;
    private List<string> suggestions = new List<string>();
    private bool isSuggestionListOpen;
    private int highlighted = -1;

    private TagEntry? flyoutEntry;
    private ElementReference flyoutElement;
    private ElementReference flyoutNameBox;
    private string flyoutName = "";
    private string flyoutInfoText = "";
    private string? flyoutError;
    private bool isConfirmingDelete;
    private string deleteConfirmText = "";

    // Focus to move once the page has drawn the change: a chip that was just added isn't there before
    private TagEntry? focusChipAfterRender;
    private bool focusFlyoutAfterRender;

    /// <summary>Gives the existing tags, and renames, recolours or deletes a tag on every todo.</summary>
    [Parameter, EditorRequired] public ITodoBoard Board { get; set; } = null!;

    /// <summary>The todo's current tags. Only read when the editor is created.</summary>
    [Parameter] public IEnumerable<NoteTag> NoteTags { get; set; } = Array.Empty<NoteTag>();

    /// <summary>Dictionary with relevant localized strings</summary>
    [Parameter, EditorRequired] public Dictionary<string, string> Strings { get; set; } = new Dictionary<string, string>();

    public IReadOnlyList<string> TagNames => entries.Select(entry => entry.Name).ToList();

    /// <summary>The colours chosen for tags that aren't on the board yet, by tag name.</summary>
    public IReadOnlyDictionary<string, string> NewTagColors => entries
        .Where(entry => entry.Id == null && entry.Color != null)
        .ToDictionary(entry => entry.Name, entry => entry.Color!, StringComparer.OrdinalIgnoreCase);

    protected override void OnInitialized()
    {
        foreach (NoteTag tag in NoteTags)
        {
            entries.Add(new TagEntry { Id = tag.Id, Name = tag.Name, Color = tag.Color });
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (focusChipAfterRender?.Chip is TagChip chip)
        {
            focusChipAfterRender = null;
            await chip.FocusName();
        }
        if (focusFlyoutAfterRender && flyoutEntry != null)
        {
            focusFlyoutAfterRender = false;
            await JS.InvokeVoidAsync("todoApp.placeFlyout", flyoutElement, flyoutEntry.Anchor);
            await flyoutNameBox.FocusAsync();
        }
    }

    /// <summary>
    /// Adds a name that's still typed in the box but wasn't submitted with Enter.
    /// Returns false, and shows why, if that name isn't valid.
    /// </summary>
    public bool CommitPendingInput()
    {
        bool committed = string.IsNullOrWhiteSpace(inputText) || tryAddTag(inputText);
        StateHasChanged();
        return committed;
    }

    private void OnInputChanged()
    {
        errorText = null;
        updateSuggestions();
    }

    private void OnInputFocus()
    {
        updateSuggestions();
    }

    private void OnInputBlur()
    {
        closeSuggestions();
    }

    private void OnInputKeyDown(KeyboardEventArgs e)
    {
        switch (e.Key)
        {
            case "Enter":
                tryAddTag(highlighted >= 0 && highlighted < suggestions.Count ? suggestions[highlighted] : inputText);
                break;
            case "ArrowDown" when suggestions.Count > 0:
                isSuggestionListOpen = true;
                highlighted = (highlighted + 1) % suggestions.Count;
                break;
            case "ArrowUp" when suggestions.Count > 0:
                isSuggestionListOpen = true;
                highlighted = highlighted <= 0 ? suggestions.Count - 1 : highlighted - 1;
                break;
            case "Escape":
                closeSuggestions();
                break;
        }
    }

    private bool tryAddTag(string text)
    {
        if (!tryNormalize(text, out string name, out string error))
        {
            errorText = error;
            return false;
        }

        // A todo can't have the same tag twice, and a tag already on the board keeps its Id and spelling
        TagEntry? entry = findEntry(name);
        if (entry == null)
        {
            NoteTag? existing = findBoardTag(name);
            entry = new TagEntry { Id = existing?.Id, Name = existing?.Name ?? name, Color = existing?.Color };
            entries.Add(entry);
        }

        inputText = "";
        errorText = null;
        closeSuggestions();

        // Move focus off the box so the suggestions close and don't cover the tags, and onto the added tag as feedback
        focusChipAfterRender = entry;
        return true;
    }

    private void updateSuggestions()
    {
        string text = inputText.Trim();
        suggestions = boardTags()
            .Select(tag => tag.Name)
            .Where(name => findEntry(name) == null)
            .Where(name => name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .ToList();

        highlighted = -1;
        isSuggestionListOpen = suggestions.Count > 0;
    }

    private void closeSuggestions()
    {
        isSuggestionListOpen = false;
        highlighted = -1;
    }

    private void removeEntry(TagEntry entry)
    {
        entries.Remove(entry);
    }

    // --- Flyout for renaming, recolouring or deleting a tag ---

    private void openFlyout(TagEntry entry)
    {
        flyoutEntry = entry;
        flyoutName = entry.Name;
        flyoutError = null;
        isConfirmingDelete = false;
        flyoutInfoText = entry.Id is Guid tagId
            ? string.Format(Strings["dialog/tag_used_on"], Board.CountNotesWithTag(tagId))
            : Strings["dialog/tag_new_info"];
        focusFlyoutAfterRender = true;
    }

    private void closeFlyout()
    {
        // Back to the tag the flyout was opened from
        focusChipAfterRender = flyoutEntry != null && entries.Contains(flyoutEntry) ? flyoutEntry : null;
        flyoutEntry = null;
    }

    private void OnFlyoutKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            closeFlyout();
        }
    }

    private void OnFlyoutNameKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            renameFlyoutEntry();
        }
    }

    private void renameFlyoutEntry()
    {
        if (flyoutEntry == null)
        {
            return;
        }

        if (!tryNormalize(flyoutName, out string name, out string error))
        {
            flyoutError = error;
            return;
        }

        // Two different tags can't share a name, whether they're on the board or only in this dialog
        bool nameTaken = findEntry(name, except: flyoutEntry) != null
                         || (flyoutEntry.Id is Guid id && findBoardTag(name, exceptId: id) != null);
        if (nameTaken)
        {
            flyoutError = string.Format(Strings["dialog/tag_name_taken"], name);
            return;
        }

        if (flyoutEntry.Id is Guid tagId)
        {
            // On the board: renames it on every todo straight away
            Board.RenameTag(tagId, name);
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

        closeFlyout();
    }

    private void OnColorPicked(AccentColorOption option)
    {
        if (flyoutEntry == null)
        {
            return;
        }

        if (flyoutEntry.Id is Guid tagId)
        {
            // On the board: changes the colour on every todo straight away
            Board.SetTagColor(tagId, option.Hex);
        }
        // Otherwise it's only in this dialog, and the colour is given to the tag when the todo is saved
        flyoutEntry.Color = option.Hex;
        closeFlyout();
    }

    private void OnDeleteClicked()
    {
        if (flyoutEntry?.Id is not Guid tagId)
        {
            return;
        }

        deleteConfirmText = string.Format(Strings["dialog/delete_tag_confirm"], flyoutEntry.Name, Board.CountNotesWithTag(tagId));
        isConfirmingDelete = true;
    }

    private void OnConfirmDeleteClicked()
    {
        if (flyoutEntry?.Id is not Guid tagId)
        {
            return;
        }

        // Removes it from every todo straight away, including this one
        Board.DeleteTag(tagId);
        removeEntry(flyoutEntry);
        flyoutEntry = null;
    }

    private void OnCancelDeleteClicked()
    {
        isConfirmingDelete = false;
    }

    // --- Lookups ---

    private List<NoteTag> boardTags()
    {
        return Board.GetTags();
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
