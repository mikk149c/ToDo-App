using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
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
        public string Title = "";
        public string PrimaryButtonText = "";
        public string TitleText = "";
        public string ContentText = "";
        public string? TitlePlaceholder;
        public string? ContentPlaceholder;
        public IEnumerable<NoteTag> InitialTags = Array.Empty<NoteTag>();
        public TagEditor? TagEditor;
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
        InvokeAsync(StateHasChanged);
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

    private void OnAddTodoClicked()
    {
        List<Guid> columns = Board.GetStatusColumnIds();
        if (columns.Count == 0)
        {
            return;
        }

        Board.CreateNote(columns[0], Strings["todo/default_title"], Strings["todo/default_description"]);
    }

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
        Board.SetStatusColumnName(columnId, columnNameInput);
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
            deleteColumnQuestion = Strings.Format("dialog/content_delete_column_consequence", noteCount);
            foreach (Guid otherColumn in otherColumns)
            {
                deleteColumnOptions.Add((otherColumn.ToString(), Strings.Format("dialog/option_move_to_column", Board.GetStatusColumnName(otherColumn))));
            }
            deleteColumnOptions.Add(("", Strings["dialog/delete_the_todos"]));
            deleteColumnChoice = deleteColumnOptions[0].Value;
        }
        else if (noteCount > 0)
        {
            deleteColumnMessage = Strings.Format("dialog/content_delete_column_no_space", columnName, noteCount);
        }

        deleteColumnId = columnId;
    }

    private void onDeleteColumnConfirmed(Guid columnId)
    {
        if (deleteColumnOptions.Count > 0 && Guid.TryParse(deleteColumnChoice, out Guid moveToColumn))
        {
            Board.RemoveColumn(columnId, moveToColumn);
        }
        else
        {
            Board.RemoveColumn(columnId);
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

        if (noteDialog.Note is ToDoNote note)
        {
            Board.SetNoteTitle(note.Id, noteDialog.TitleText);
            Board.SetNoteContent(note.Id, noteDialog.ContentText);
            Board.SetNoteTags(note.Id, tagEditor.TagNames, tagEditor.NewTagColors);
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
        Board.RemoveNote(note.Id);
        closeDialogs();
    }

    // --- Attachments ---

    private async Task onAttachFileChosen(ToDoNote note, InputFileChangeEventArgs e)
    {
        try
        {
            await using Stream upload = e.File.OpenReadStream(AttachmentStore.MaxFileSize);
            string path = await Attachments.SaveAsync(upload, e.File.Name);
            Board.SetNoteFileAttachment(note.Id, path);
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
            Board.RemoveNoteFileAttachment(note.Id);
        }
        catch (Exception exception)
        {
            errorDialog = (Strings["error/failed_to_remove_attached_file"], exception.Message);
        }
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
            int dropIndex = cardIndex ?? lastIndex;
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
            "dialog/tag_name_taken", "dialog/tag_colour", "dialog/rename", "dialog/delete", "dialog/cancel", "dialog/delete_tag_confirm"
        };
        // A missing translation shows the key instead of crashing the dialog
        return keys.ToDictionary(key => key, key => Strings[key]);
    }
}
