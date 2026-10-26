using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using ToDo_App.Controller;
using ToDo_App.Interfaces;
using ToDo_App.Web.Appearance;
using ToDo_App.Web.Board;
using ToDo_App.Web.Components.Controls;
using ToDo_App.Web.Localization;

namespace ToDo_App.Web.Components.Pages;

/// <summary>
/// The board: toolbar, columns and todo cards, and the dialogs to edit them. The web version of
/// <c>ToDo-App.UI/Views/MainPage</c>, using the same <see cref="ITodoBoard"/> calls and events.
/// </summary>
public partial class MainPage
{
    private const long MaxImportSize = 10 * 1024 * 1024;

    // The state of the note dialog, used both to add and to edit a todo
    private sealed class NoteDialogState
    {
        public Guid? ColumnId;                // set when adding
        public ToDoNote? Note;                // set when editing
        public OpenedNote? Opened;            // the note as it was when the dialog opened, when editing
        public string Title = "";
        public string PrimaryButtonText = "";
        public string TitleText = "";
        public string ContentText = "";
        public string? TitlePlaceholder;
        public string? ContentPlaceholder;
        public IEnumerable<NoteTag> InitialTags = Array.Empty<NoteTag>();
        public TagEditor? TagEditor;
    }

    // The steps of the trash bin dialog: the list, then asking where to restore a todo, or asking before deleting
    private enum TrashStep
    {
        List,
        Restore,
        Delete,
        Empty
    }

    private bool isReady;
    private bool isImporting;
    private Guid? draggedNoteId;
    private Guid? draggedColumnId;

    private bool isAddColumnDialogOpen;
    private Guid? editColumnId;
    private string columnNameInput = "";
    private Guid? deleteColumnId;
    private string deleteColumnMessage = "";
    private string deleteColumnQuestion = "";
    private List<(string Value, string Label)> deleteColumnOptions = new();
    private string deleteColumnChoice = "";
    private NoteDialogState? noteDialog;
    private ToDoNote? deleteNoteDialog;
    private (string Title, string Message)? errorDialog;

    // Which todos are shown. Only affects what this tab shows, never the board
    private readonly TagFilter tagFilter = new TagFilter();
    private bool isFilterOpen;

    private bool isTrashOpen;
    private TrashStep trashStep;
    private Guid? trashNoteId;
    private readonly HashSet<Guid> expandedTrashNotes = new HashSet<Guid>();

    [Inject] private BoardHost Host { get; set; } = null!;
    [Inject] private AttachmentStore Attachments { get; set; } = null!;
    [Inject] private UiStrings Strings { get; set; } = null!;
    [Inject] private AppearanceManager Appearance { get; set; } = null!;
    [Inject] private BrowserPreferences Preferences { get; set; } = null!;

    private ITodoBoard Board => Host.Board;

    protected override void OnInitialized()
    {
        // The board is shared by every open tab, so a change made in any of them redraws this one.
        // The page draws straight from the board, so every event is handled the same way
        Board.ColumnCreated += onBoardEvent;
        Board.ColumnNameChanged += onBoardEvent;
        Board.ColumnRemoved += onBoardEvent;
        Board.ColumnContentChanged += onBoardEvent;
        Board.ColumnOrderChanged += onBoardEvent;
        Board.TagChanged += onBoardEvent;
        Host.SaveCompleted += onBoardEvent;
        Strings.Changed += onBoardEvent;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        // The language and appearance are remembered in the browser, so they're read once it's connected
        Strings.SetLanguage(await Preferences.LoadLanguageAsync());
        await Appearance.AttachAsync();
        await Appearance.SetLanguageAsync(Strings.CurrentLanguage);
        isReady = true;

        if (Host.LoadError != null)
        {
            errorDialog = (Strings["error/failed_to_load_board"], Host.LoadError);
        }
        StateHasChanged();
    }

    private void onBoardEvent(object? sender, EventArgs eventArgs)
    {
        // Events can come from another tab's connection, so redraw on this tab's own
        InvokeAsync(() =>
        {
            // The filter turns itself off when the last tag it was filtering by is gone
            tagFilter.Prune(Board.GetTags());
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        Board.ColumnCreated -= onBoardEvent;
        Board.ColumnNameChanged -= onBoardEvent;
        Board.ColumnRemoved -= onBoardEvent;
        Board.ColumnContentChanged -= onBoardEvent;
        Board.ColumnOrderChanged -= onBoardEvent;
        Board.TagChanged -= onBoardEvent;
        Host.SaveCompleted -= onBoardEvent;
        Strings.Changed -= onBoardEvent;
    }

    // --- Toolbar ---

    private async Task onImportBoardChosen(InputFileChangeEventArgs e)
    {
        // Disable the button to avoid importing twice
        isImporting = true;
        string tempPath = Path.Combine(Path.GetTempPath(), $"todo-import-{Guid.NewGuid()}.json");
        try
        {
            await using (FileStream file = File.Create(tempPath))
            {
                await e.File.OpenReadStream(MaxImportSize).CopyToAsync(file);
            }
            // The controller raises ColumnRemoved for the current columns, so every tab redraws itself
            Board.LoadBoardFromJson(tempPath);
        }
        catch (Exception exception)
        {
            errorDialog = (Strings["error/failed_to_import_board"], exception.Message);
        }
        finally
        {
            File.Delete(tempPath);
            isImporting = false;
        }
    }

    private async Task onLanguageSelectionChanged(ChangeEventArgs e)
    {
        string language = e.Value?.ToString() ?? UiStrings.DefaultLanguage;
        Strings.SetLanguage(language);
        await Appearance.SetLanguageAsync(Strings.CurrentLanguage);
        await Preferences.SaveLanguageAsync(Strings.CurrentLanguage);
    }

    // --- Columns ---

    private void OnClickAddColumn()
    {
        columnNameInput = "";
        isAddColumnDialogOpen = true;
    }

    private void onAddColumnConfirmed()
    {
        Board.CreateColumn(columnNameInput);
        closeDialogs();
    }

    private void onEditColumnClicked(Guid columnId)
    {
        columnNameInput = Board.GetStatusColumnName(columnId);
        editColumnId = columnId;
    }

    private void onEditColumnConfirmed(Guid columnId)
    {
        Board.UpdateColumn(columnId, columnNameInput);
        closeDialogs();
    }

    private void onDeleteColumnClicked(Guid columnId)
    {
        string columnName = Board.GetStatusColumnName(columnId);
        int noteCount = Board.GetNotesInColumn(columnId).Count;
        List<Guid> otherColumns = Board.GetStatusColumnIds().FindAll(id => id != columnId);

        deleteColumnMessage = Strings.Format("dialog/content_delete_column", columnName);
        deleteColumnOptions = new();

        // Only ask what to do with the ToDos when there are some, and somewhere to move them
        if (noteCount > 0 && otherColumns.Count > 0)
        {
            deleteColumnQuestion = Strings.FormatPlural("dialog/content_delete_column_consequence", noteCount, 0);
            foreach (Guid otherColumn in otherColumns)
            {
                deleteColumnOptions.Add((otherColumn.ToString(), Strings.Format("dialog/option_move_to_column", Board.GetStatusColumnName(otherColumn))));
            }
            deleteColumnOptions.Add(("", Strings.FormatPlural("dialog/delete_the_todos", noteCount, 0)));
            deleteColumnChoice = deleteColumnOptions[0].Value;
        }
        else if (noteCount > 0)
        {
            deleteColumnMessage = Strings.FormatPlural("dialog/content_delete_column_no_space", noteCount, 1, columnName);
        }

        deleteColumnId = columnId;
    }

    private void onDeleteColumnConfirmed(Guid columnId)
    {
        if (deleteColumnOptions.Count > 0 && Guid.TryParse(deleteColumnChoice, out Guid moveToColumn))
        {
            Board.DeleteColumn(columnId, moveToColumn);
        }
        else
        {
            Board.DeleteColumn(columnId);
        }
        closeDialogs();
    }

    // --- Todos ---

    private void onAddTodoToColumnClicked(Guid columnId)
    {
        noteDialog = new NoteDialogState
        {
            ColumnId = columnId,
            Title = Strings["dialog/title_add_new_note"],
            PrimaryButtonText = Strings["dialog/add"],
            TitlePlaceholder = Strings["todo/default_title"],
            ContentPlaceholder = Strings["todo/default_description"]
        };
    }

    private void onEditTodoClicked(ToDoNote note)
    {
        noteDialog = new NoteDialogState
        {
            Note = note,
            Opened = OpenedNote.Of(note),
            Title = Strings["dialog/title_edit_note"],
            PrimaryButtonText = Strings["dialog/save"],
            TitleText = note.Title,
            ContentText = note.Content,
            InitialTags = note.Tags.ToList()
        };
    }

    // Keep the dialog open if a typed tag isn't valid, so the user can see why
    private bool commitNoteDialogTags()
    {
        return noteDialog?.TagEditor?.CommitPendingInput() ?? true;
    }

    private void onNoteDialogConfirmed()
    {
        if (noteDialog?.TagEditor is not TagEditor tagEditor)
        {
            return;
        }

        if (noteDialog is { Note: ToDoNote note, Opened: OpenedNote opened })
        {
            // Only what the user changed, so a change made in another tab or app meanwhile isn't undone
            var changes = NoteChanges.Since(opened, noteDialog.TitleText, noteDialog.ContentText, tagEditor.TagNames, tagEditor.NewTagColors);
            if (changes.HasChanges())
            {
                Board.UpdateNote(note.Id, changes);
            }
        }
        else if (noteDialog.ColumnId is Guid columnId)
        {
            // Leaving a field empty keeps the placeholder text, as it did when the fields were prefilled
            string defaultTitle = Strings["todo/default_title"];
            string defaultContent = Strings["todo/default_description"];
            Board.CreateNote(
                columnId,
                string.IsNullOrWhiteSpace(noteDialog.TitleText) ? defaultTitle : noteDialog.TitleText,
                string.IsNullOrWhiteSpace(noteDialog.ContentText) ? defaultContent : noteDialog.ContentText,
                tagNames: tagEditor.TagNames,
                newTagColors: tagEditor.NewTagColors);
        }
        closeDialogs();
    }

    private void onDeleteTodoConfirmed(ToDoNote note)
    {
        Board.TrashNote(note.Id);
        closeDialogs();
    }

    // --- Attachments ---

    private async Task onAttachFileChosen(ToDoNote note, InputFileChangeEventArgs e)
    {
        try
        {
            await using Stream upload = e.File.OpenReadStream(AttachmentStore.MaxFileSize);
            string path = await Attachments.SaveAsync(upload, e.File.Name);
            Board.UpdateNote(note.Id, new NoteChanges { File = new AttachedFile(Path.GetFileName(path), path) });
        }
        catch (Exception exception)
        {
            errorDialog = (Strings["error/failed_to_attach_file"], exception.Message);
        }
    }

    private void onAttachmentOnPcClicked(ToDoNote note)
    {
        errorDialog = (Strings["error/failed_to_open_attached_file"], Strings.Format("error/attachment_only_on_pc", note.File?.Name ?? ""));
    }

    private void onRemoveAttachmentClicked(ToDoNote note)
    {
        try
        {
            Board.UpdateNote(note.Id, new NoteChanges { RemoveFile = true });
        }
        catch (Exception exception)
        {
            errorDialog = (Strings["error/failed_to_remove_attached_file"], exception.Message);
        }
    }

    // --- Filter by tags ---

    private string filterButtonText => tagFilter.IsActive
        ? Strings.FormatPlural("filter/button_active", tagFilter.TagIds.Count, 0)
        : Strings["filter/button"];

    private void toggleFilter()
    {
        isFilterOpen = !isFilterOpen;
    }

    private void closeFilter()
    {
        isFilterOpen = false;
    }

    private void onFilterKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            closeFilter();
        }
    }

    private void onFilterTagToggled(Guid tagId, bool isSelected)
    {
        if (isSelected)
        {
            tagFilter.Select(tagId);
        }
        else
        {
            tagFilter.Deselect(tagId);
        }
    }

    private void onClearFilterClicked()
    {
        tagFilter.Clear();
        isFilterOpen = false;
    }

    // --- Trash bin ---

    private List<ToDoNote> trashNotes()
    {
        return Board.GetTrashBinNotes().ToList();
    }

    private ToDoNote? selectedTrashNote()
    {
        return trashNoteId is Guid id ? trashNotes().Find(note => note.Id == id) : null;
    }

    private void onTrashBinClicked()
    {
        backToTrashList();
        isTrashOpen = true;
    }

    private void backToTrashList()
    {
        trashStep = TrashStep.List;
        trashNoteId = null;
    }

    private void toggleTrashNote(Guid noteId)
    {
        if (!expandedTrashNotes.Remove(noteId))
        {
            expandedTrashNotes.Add(noteId);
        }
    }

    private void onTrashRestoreClicked(ToDoNote note)
    {
        trashNoteId = note.Id;
        trashStep = TrashStep.Restore;
    }

    private void onTrashDeleteClicked(ToDoNote note)
    {
        trashNoteId = note.Id;
        trashStep = TrashStep.Delete;
    }

    private void onEmptyTrashClicked()
    {
        trashNoteId = null;
        trashStep = TrashStep.Empty;
    }

    private void onTrashRestoreConfirmed(Guid columnId)
    {
        if (trashNoteId is Guid noteId)
        {
            changeTrash(() => Board.RestoreNote(noteId, columnId));
        }
    }

    private void onTrashDeleteConfirmed()
    {
        if (trashNoteId is Guid noteId)
        {
            changeTrash(() => Board.DeleteNote(noteId));
        }
    }

    private void onEmptyTrashConfirmed()
    {
        changeTrash(Board.EmptyTrash);
    }

    // Another tab may have emptied or restored the same todo, so a refused change is shown instead of crashing
    private void changeTrash(Action change)
    {
        try
        {
            change();
        }
        catch (Exception exception) when (exception is ArgumentException or KeyNotFoundException or InvalidOperationException)
        {
            errorDialog = (Strings["button/trash_bin"], exception.Message);
        }
        backToTrashList();
    }

    // --- Drag and drop ---

    private void onNoteDragStarting(ToDoNote note)
    {
        draggedNoteId = note.Id;
        draggedColumnId = null;
    }

    private void onColumnDragStarting(Guid columnId)
    {
        draggedColumnId = columnId;
        draggedNoteId = null;
    }

    private void onDragEnded()
    {
        draggedNoteId = null;
        draggedColumnId = null;
    }

    /// <summary>
    /// Something was dropped on a column, or on the card at <paramref name="cardIndex"/> in it.
    /// A todo takes the place of the card it's dropped on, or goes last when dropped elsewhere in the column.
    /// A column takes the place of the column it's dropped on.
    /// </summary>
    private void onColumnDrop(Guid columnId, int? cardIndex)
    {
        if (draggedNoteId is Guid noteId)
        {
            List<ToDoNote> notes = Board.GetNotesInColumn(columnId);
            int currentIndex = notes.FindIndex(note => note.Id == noteId);
            int lastIndex = currentIndex == -1 ? notes.Count : notes.Count - 1;
            // The card is a position among the todos that are shown, so hidden todos stay where they are
            int dropIndex = cardIndex is int shownIndex ? tagFilter.ToColumnIndex(notes, shownIndex) : lastIndex;
            if (dropIndex != currentIndex)
            {
                Board.MoveNoteToColumn(noteId, columnId, dropIndex);
            }
        }
        else if (draggedColumnId is Guid movedColumnId && movedColumnId != columnId)
        {
            Board.MoveColumn(movedColumnId, Board.GetStatusColumnIds().IndexOf(columnId));
        }
        onDragEnded();
    }

    // --- Helpers ---

    private void closeDialogs()
    {
        isAddColumnDialogOpen = false;
        editColumnId = null;
        deleteColumnId = null;
        noteDialog = null;
        deleteNoteDialog = null;
        isTrashOpen = false;
        backToTrashList();
    }

    private static void onEnter(KeyboardEventArgs e, Action action)
    {
        if (e.Key == "Enter")
        {
            action();
        }
    }

    private Dictionary<string, string> tagEditorStrings()
    {
        string[] keys =
        {
            "todo/property_tags", "dialog/add_tag_placeholder", "dialog/remove_tag", "dialog/edit_tag",
            "dialog/new_tag_tooltip", "dialog/tag_name", "dialog/tag_used_on", "dialog/tag_new_info",
            "dialog/tag_name_taken", "dialog/tag_colour", "dialog/rename", "dialog/delete", "dialog/cancel", "dialog/delete_tag_confirm",
            "dialog/merge", "dialog/merge_tag_confirm"
        };
        // A missing translation shows the key instead of crashing the dialog
        Dictionary<string, string> texts = keys.ToDictionary(key => key, key => Strings[key]);
        // The editor picks the plural forms of its texts by language
        texts["lang_code"] = Strings.CurrentLanguage;
        return texts;
    }
}
