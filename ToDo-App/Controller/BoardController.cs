using ToDo_App.Interfaces;

namespace ToDo_App.Controller;

/// <summary>
/// The board, its events and how each change is applied, shared by every kind of controller. A child decides when a
/// change is applied: <see cref="LocalBoardController"/> applies it straight away, and a remote controller sends it
/// to a server and applies it when the server sends it back.
/// </summary>
/// <remarks>
/// Ids for new columns, notes and tags are made here, before a change goes anywhere, so every copy of the board
/// gives them the same Id.
/// </para>
/// </remarks>
public abstract class BoardController : ITodoBoard
{
    private ToDoBoard _board = new ToDoBoard();

    public event EventHandler? ColumnCreated;
    public event EventHandler? ColumnNameChanged;
    public event EventHandler? ColumnRemoved;
    public event EventHandler? ColumnContentChanged;
    public event EventHandler? BoardChanged;
    public event EventHandler? ColumnOrderChanged;
    public event EventHandler? TagChanged;
    public event EventHandler? TrashBinChanged;
    private void onColumnCreated(ColumnCreatedEventArgs e)
    {
        ColumnCreated?.Invoke(this, e);
    }

    private void onColumnNameChanged(ColumnNameChangedEventArgs e)
    {
        ColumnNameChanged?.Invoke(this, e);
    }

    private void onColumnRemoved(ColumnRemovedEventArgs e)
    {
        ColumnRemoved?.Invoke(this, e);
    }

    private void onColumnContentChanged(ColumnContentChangedEventArgs e)
    {
        ColumnContentChanged?.Invoke(this, e);
    }

    private void onBoardChanged(BoardChangedEventArgs e)
    {
        BoardChanged?.Invoke(this, e);
    }
    private void onColumnOrderChanged(ColumnOrderChangedEventArgs e)
    {
        ColumnOrderChanged?.Invoke(this, e);
    }
    private void onTagChanged(TagChangedEventArgs e)
    {
        TagChanged?.Invoke(this, e);
    }
    private void onTrashBinChanged()
    {
        TrashBinChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The board itself, for a child to save or send.</summary>
    protected ToDoBoard Board => _board;

    // --- Reads ---

    public List<Guid> GetStatusColumnIds()
    {
        var Ids = new List<Guid>();
        foreach (var column in _board.StatusColumns)
        {
            Ids.Add(column.Id);
        }

        return Ids;
    }

    public String GetStatusColumnName(Guid columnId)
    {
        int columnIndex = _board.GetColumnIndexFromId(columnId);
        return _board.StatusColumns[columnIndex].Title;
    }

    public List<ToDoNote> GetNotesInColumn(Guid columnId)
    {
        int columnIndex = _board.GetColumnIndexFromId(columnId);
        return _board.StatusColumns[columnIndex].Notes;
    }

    public List<NoteTag> GetTags()
    {
        return _board.GetTags();
    }

    public int CountNotesWithTag(Guid tagId)
    {
        return _board.CountNotesWithTag(tagId);
    }

    public void SaveBoardToJson(string jsonPath)
    {
        var storeJson = new StoreJson<ToDoBoard>(jsonPath);
        storeJson.Save(_board);
    }

    // --- Changes. Each is a verb and a thing: Create, Update, Delete, or Move, Trash, Restore, Merge, Empty ---

    public virtual void UpdateColumn(Guid columnId, string name)
    {
        int columnIndex = _board.GetColumnIndexFromId(columnId);
        _board.StatusColumns[columnIndex].Title = name;
        onColumnNameChanged(new ColumnNameChangedEventArgs(columnId, GetStatusColumnName(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public Guid CreateColumn(string name)
    {
        Guid columnId = Guid.NewGuid();
        CreateColumn(columnId, name);
        return columnId;
    }

    protected virtual void CreateColumn(Guid columnId, string name)
    {
        _board.CreateColumn(name, columnId);
        onColumnCreated(new ColumnCreatedEventArgs(columnId, GetStatusColumnName(columnId), GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    /// <summary>Deletes a column, moving its notes to the trash, or to <paramref name="moveNotesToColumnId"/> when given.</summary>
    public virtual void DeleteColumn(Guid columnId, Guid? moveNotesToColumnId = null)
    {
        if (moveNotesToColumnId is Guid targetColumnId)
        {
            if (columnId == targetColumnId)
            {
                throw new ArgumentException("Notes cannot be moved to the column that is being removed");
            }

            _board.MoveAllNotes(columnId, targetColumnId);
            onColumnContentChanged(new ColumnContentChangedEventArgs(targetColumnId, GetNotesInColumn(targetColumnId)));
        }
        else
        {
            _board.AddColumnNotesToTrashBin(columnId);
            onTrashBinChanged();
        }

        _board.RemoveColumn(columnId);
        onColumnRemoved(new ColumnRemovedEventArgs(columnId));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public virtual void MoveColumn(Guid columnId, int targetIndex)
    {
        _board.MoveColumn(columnId, targetIndex);
        onColumnOrderChanged(new ColumnOrderChangedEventArgs(GetStatusColumnIds()));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public Guid CreateNote(Guid columnId, string title, string content, AttachedFile? file = null, IEnumerable<string>? tagNames = null, IReadOnlyDictionary<string, string>? newTagColors = null)
    {
        Guid noteId = Guid.NewGuid();
        List<string> names = tagNames?.ToList() ?? [];
        CreateNote(columnId, noteId, title, content, file, names, newTagColors, newTagIdsFor(names));
        return noteId;
    }

    /// <param name="newTagIds">The Id for each tag name that isn't a tag on the board yet (tag name to Id).</param>
    protected virtual void CreateNote(Guid columnId, Guid noteId, string title, string content, AttachedFile? file,
        IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors, IReadOnlyDictionary<string, Guid> newTagIds)
    {
        // Resolve tags before adding the note, so an invalid tag name leaves the board unchanged
        var note = new ToDoNote(title, content, file) { Id = noteId, Tags = _board.ResolveTags(tagNames, newTagColors, newTagIds) };
        _board.AddNote(columnId, note);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public virtual void UpdateNoteTitle(Guid noteId, string title)
    {
        _board.SetNoteTitle(noteId, title);
        notifyNoteChanged(noteId);
    }

    public virtual void UpdateNoteContent(Guid noteId, string content)
    {
        _board.SetNoteContent(noteId, content);
        notifyNoteChanged(noteId);
    }

    /// <summary>Replaces a note's tags, by name.</summary>
    /// <param name="newTagColors">The colour for each tag name that isn't a tag on the board yet (tag name to "#RRGGBB").</param>
    public void UpdateNoteTags(Guid noteId, IEnumerable<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors = null)
    {
        List<string> names = tagNames.ToList();
        UpdateNoteTags(noteId, names, newTagColors, newTagIdsFor(names));
    }

    /// <param name="newTagIds">The Id for each tag name that isn't a tag on the board yet (tag name to Id).</param>
    protected virtual void UpdateNoteTags(Guid noteId, IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors,
        IReadOnlyDictionary<string, Guid> newTagIds)
    {
        _board.SetNoteTags(noteId, tagNames, newTagColors, newTagIds);
        notifyNoteChanged(noteId);
    }

    /// <summary>Attaches a file to a note, replacing the one it has.</summary>
    public virtual void UpdateNoteFile(Guid noteId, AttachedFile file)
    {
        _board.SetNoteFile(noteId, file);
        notifyNoteChanged(noteId);
    }

    /// <summary>Removes a note's file.</summary>
    public virtual void DeleteNoteFile(Guid noteId)
    {
        _board.SetNoteFile(noteId, null);
        notifyNoteChanged(noteId);
    }

    public virtual void UpdateTagName(Guid tagId, string name)
    {
        _board.RenameTag(tagId, name);
        notifyTagChanged(tagId);
    }

    public virtual void UpdateTagColor(Guid tagId, string color)
    {
        _board.SetTagColor(tagId, color);
        notifyTagChanged(tagId);
    }

    public virtual void DeleteTag(Guid tagId)
    {
        List<Guid> affectedColumnIds = _board.GetColumnIdsWithTag(tagId);
        _board.DeleteTag(tagId);
        foreach (Guid columnId in affectedColumnIds)
        {
            onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        }
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public virtual void MergeTags(Guid sourceTagId, Guid targetTagId)
    {
        // Merging changes the tags on the cards, so the affected columns are redrawn
        List<Guid> affectedColumnIds = _board.GetColumnIdsWithTag(sourceTagId);
        _board.MergeTags(sourceTagId, targetTagId);
        foreach (Guid columnId in affectedColumnIds)
        {
            onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        }
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public virtual void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex)
    {
        moveNoteAndNotify(noteId, () => _board.MoveNoteToColumn(noteId, targetColumnId, targetIndex));
    }

    /// <summary>Moves a note to the trash, from where it can be restored.</summary>
    public virtual void TrashNote(Guid noteId)
    {
        Guid columnId = _board.GetColumnIdFromNoteId(noteId);
        _board.AddNoteToTrashBin(noteId);
        _board.RemoveNote(noteId);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
        onTrashBinChanged();
    }

    /// <summary>Deletes a note in the trash for good.</summary>
    public virtual void DeleteNote(Guid noteId)
    {
        _board.RemoveNoteFromTrashBin(noteId);
        onTrashBinChanged();
    }

    public virtual void RestoreNote(Guid noteId, Guid columnId)
    {
        _board.RestoreNoteFromTrashBin(noteId, columnId);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
        onTrashBinChanged();
    }

    public virtual void EmptyTrash()
    {
        _board.EmptyTrashBin();
        onTrashBinChanged();
    }

    public List<ToDoNote> GetTrashBinNotes() { return _board.TrashBin.Notes; }

    public void LoadBoardFromJson(string jsonPath)
    {
        var storeJson = new StoreJson<ToDoBoard>(jsonPath);
        // Load first, so a file that can't be read leaves the current board and UI untouched
        ReplaceBoard(storeJson.Load());
    }

    /// <summary>Replaces the whole board, as importing a board file does.</summary>
    protected virtual void ReplaceBoard(ToDoBoard loadedBoard)
    {
        loadedBoard.NormalizeTags();
        List<Guid> replacedColumnIds = GetStatusColumnIds();
        _board = loadedBoard;

        foreach (Guid columnId in replacedColumnIds)
        {
            onColumnRemoved(new ColumnRemovedEventArgs(columnId));
        }
        foreach (var column in _board.StatusColumns)
        {
            onColumnCreated(new ColumnCreatedEventArgs(column.Id, column.Title, column.Notes));
        }
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    // An Id for each tag name, used only by the names that aren't a tag yet
    private static Dictionary<string, Guid> newTagIdsFor(IEnumerable<string> tagNames)
    {
        return tagNames
            .Select(name => name?.Trim() ?? "")
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(name => name, _ => Guid.NewGuid(), StringComparer.OrdinalIgnoreCase);
    }

    private void notifyNoteChanged(Guid noteId)
    {
        Guid columnId = _board.GetColumnIdFromNoteId(noteId);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    // One event for the tag, so every card showing it can update, whichever columns they're in
    private void notifyTagChanged(Guid tagId)
    {
        NoteTag tag = _board.GetTags().Single(candidate => candidate.Id == tagId);
        onTagChanged(new TagChangedEventArgs(tagId, tag.Name, tag.Color));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    private void moveNoteAndNotify(Guid noteId, Action move)
    {
        Guid sourceColumnId = _board.GetColumnIdFromNoteId(noteId);
        move();
        Guid targetColumnId = _board.GetColumnIdFromNoteId(noteId);

        onColumnContentChanged(new ColumnContentChangedEventArgs(sourceColumnId, GetNotesInColumn(sourceColumnId)));
        if (targetColumnId != sourceColumnId)
        {
            onColumnContentChanged(new ColumnContentChangedEventArgs(targetColumnId, GetNotesInColumn(targetColumnId)));
        }
        onBoardChanged(new BoardChangedEventArgs(_board));
    }
}
