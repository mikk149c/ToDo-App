namespace ToDo_App.Interfaces;

/// <summary>
/// A board of columns, notes and tags. Every change is a verb and a thing: Create, Update and Delete, or Move, Trash,
/// Restore, Merge and Empty where those don't fit.
/// </summary>
public interface ITodoBoard
{
    public event EventHandler ColumnCreated;
    public event EventHandler ColumnNameChanged;
    public event EventHandler ColumnRemoved;
    public event EventHandler ColumnContentChanged;
    public event EventHandler BoardChanged;
    public event EventHandler? ColumnOrderChanged;
    public event EventHandler? TagChanged;
    public event EventHandler? TrashBinChanged;

    // --- Reads ---

    public List<Guid> GetStatusColumnIds();

    public String GetStatusColumnName(Guid columnId);

    public List<ToDoNote> GetNotesInColumn(Guid columnId);

    public List<ToDoNote> GetTrashBinNotes();

    public List<NoteTag> GetTags();

    public int CountNotesWithTag(Guid tagId);

    // --- Columns ---

    public Guid CreateColumn(string name);

    public void UpdateColumn(Guid columnId, string name);

    public void DeleteColumn(Guid columnId, Guid? moveNotesToColumnId = null);

    public void MoveColumn(Guid columnId, int targetIndex);

    // --- Notes ---

    public Guid CreateNote(Guid columnId, string title, string content, AttachedFile? file = null, IEnumerable<string>? tagNames = null, IReadOnlyDictionary<string, string>? newTagColors = null);

    public void UpdateNoteTitle(Guid noteId, string title);

    public void UpdateNoteContent(Guid noteId, string content);

    public void UpdateNoteTags(Guid noteId, IEnumerable<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors = null);

    public void UpdateNoteFile(Guid noteId, FileReference fileReference);

    public void DeleteNoteFile(Guid noteId);

    public void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex);

    public void TrashNote(Guid noteId);

    public void RestoreNote(Guid noteId, Guid columnId);

    public void DeleteNote(Guid noteId);

    public void EmptyTrash();

    // --- Tags ---

    public void UpdateTagName(Guid tagId, string name);

    public void UpdateTagColor(Guid tagId, string color);

    public void MergeTags(Guid sourceTagId, Guid targetTagId);

    public void DeleteTag(Guid tagId);

    // --- The whole board ---

    public void SaveBoardToJson(string jsonPath);

    public void LoadBoardFromJson(string jsonPath);
    
    // --- Attached Files ---
    public bool IsFileStoredOnServer(string fullPath);
}
