using ToDo_App.Interfaces;

namespace ToDo_App.Controller;

/// <summary>
/// The board, its events and how each change is applied, shared by every kind of controller. A child decides when a
/// change is applied: <see cref="LocalBoardController"/> applies it straight away, and a remote controller sends it
/// to a server and applies it when the server sends it back.
/// </summary>
/// <remarks>
/// Each change is an abstract method, which every child implements, and a protected Apply method with the same
/// parameters, which changes the board and raises the events. Apply methods only call other Apply methods and
/// private helpers, never the abstract ones, so applying a change never sends it anywhere again.
/// <para>
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

    /// <summary>The board itself, for a child to save or send. Changes go through the Apply methods.</summary>
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

    // --- Changes. Each child implements the abstract ones ---

    public abstract void SetStatusColumnName(Guid columnId, string newName);

    public Guid CreateColumn(string name)
    {
        Guid columnId = Guid.NewGuid();
        CreateColumn(columnId, name);
        return columnId;
    }

    protected abstract void CreateColumn(Guid columnId, string name);

    public void RemoveColumn(Guid columnId)
    {
        RemoveColumn(columnId, (Guid?)null);
    }

    public void RemoveColumn(Guid columnId, Guid moveNotesToColumnId)
    {
        RemoveColumn(columnId, (Guid?)moveNotesToColumnId);
    }

    /// <summary>Removes a column, deleting its notes, or moving them to <paramref name="moveNotesToColumnId"/> when given.</summary>
    protected abstract void RemoveColumn(Guid columnId, Guid? moveNotesToColumnId);

    public void MoveColumnLeft(Guid columnId)
    {
        throw new NotImplementedException();
    }

    public void MoveColumnRight(Guid columnId)
    {
        throw new NotImplementedException();
    }

    public abstract void MoveColumn(Guid columnId, int targetIndex);

    public Guid CreateNote(Guid columnId, string title, string content, AttachedFile? file = null, IEnumerable<string>? tagNames = null, IReadOnlyDictionary<string, string>? newTagColors = null)
    {
        Guid noteId = Guid.NewGuid();
        List<string> names = tagNames?.ToList() ?? [];
        CreateNote(columnId, noteId, title, content, file, names, newTagColors, newTagIdsFor(names));
        return noteId;
    }

    /// <param name="newTagIds">The Id for each tag name that isn't a tag on the board yet (tag name to Id).</param>
    protected abstract void CreateNote(Guid columnId, Guid noteId, string title, string content, AttachedFile? file,
        IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors, IReadOnlyDictionary<string, Guid> newTagIds);

    public abstract void SetNoteContent(Guid id, string newContent);

    public abstract void SetNoteTitle(Guid id, string newTitle);

    public void SetNoteTags(Guid id, IEnumerable<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors = null)
    {
        List<string> names = tagNames.ToList();
        SetNoteTags(id, names, newTagColors, newTagIdsFor(names));
    }

    /// <param name="newTagIds">The Id for each tag name that isn't a tag on the board yet (tag name to Id).</param>
    protected abstract void SetNoteTags(Guid id, IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors,
        IReadOnlyDictionary<string, Guid> newTagIds);

    public abstract void RenameTag(Guid tagId, string newName);

    public abstract void SetTagColor(Guid tagId, string color);

    public abstract void DeleteTag(Guid tagId);

    public void SetNoteFileAttachment(Guid id, string newFilePath)
    {
        // The name is read here, on the computer the file is on
        var fileInfo = new FileInfo(newFilePath);
        SetNoteFileAttachment(id, new AttachedFile(fileInfo.Name, newFilePath));
    }

    protected abstract void SetNoteFileAttachment(Guid id, AttachedFile file);

    public abstract void RemoveNoteFileAttachment(Guid id);

    public abstract void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos);

    public abstract void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex);

    public abstract void MoveNoteDown(Guid noteId);

    public abstract void MoveNoteUp(Guid noteId);

    public abstract void MoveNoteRight(Guid noteId);

    public abstract void MoveNoteLeft(Guid noteId);

    public abstract void RemoveNote(Guid noteId);

    public void LoadBoardFromJson(string jsonPath)
    {
        var storeJson = new StoreJson<ToDoBoard>(jsonPath);
        // Load first, so a file that can't be read leaves the current board and UI untouched
        ReplaceBoard(storeJson.Load());
    }

    /// <summary>Replaces the whole board, as importing a board file does.</summary>
    protected abstract void ReplaceBoard(ToDoBoard board);

    // An Id for each tag name, used only by the names that aren't a tag yet
    private static Dictionary<string, Guid> newTagIdsFor(IEnumerable<string> tagNames)
    {
        return tagNames
            .Select(name => name?.Trim() ?? "")
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(name => name, _ => Guid.NewGuid(), StringComparer.OrdinalIgnoreCase);
    }

    // --- How each change is applied to the board, and which events it raises ---

    protected void ApplySetStatusColumnName(Guid columnId, string newName)
    {
        int columnIndex = _board.GetColumnIndexFromId(columnId);
        _board.StatusColumns[columnIndex].Title = newName;
        onColumnNameChanged(new ColumnNameChangedEventArgs(columnId, GetStatusColumnName(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    protected void ApplyCreateColumn(Guid columnId, string name)
    {
        _board.CreateColumn(name, columnId);
        onColumnCreated(new ColumnCreatedEventArgs(columnId, GetStatusColumnName(columnId), GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    protected void ApplyRemoveColumn(Guid columnId, Guid? moveNotesToColumnId)
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

        _board.RemoveColumn(columnId);
        onColumnRemoved(new ColumnRemovedEventArgs(columnId));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    protected void ApplyMoveColumn(Guid columnId, int targetIndex)
    {
        _board.MoveColumn(columnId, targetIndex);
        onColumnOrderChanged(new ColumnOrderChangedEventArgs(GetStatusColumnIds()));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    protected void ApplyCreateNote(Guid columnId, Guid noteId, string title, string content, AttachedFile? file,
        IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors, IReadOnlyDictionary<string, Guid> newTagIds)
    {
        // Resolve tags before adding the note, so an invalid tag name leaves the board unchanged
        var note = new ToDoNote(title, content, file) { Id = noteId, Tags = _board.ResolveTags(tagNames, newTagColors, newTagIds) };
        _board.AddNote(columnId, note);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    protected void ApplySetNoteContent(Guid id, string newContent)
    {
        _board.SetNoteContent(id, newContent);
        notifyNoteChanged(id);
    }

    protected void ApplySetNoteTitle(Guid id, string newTitle)
    {
        _board.SetNoteTitle(id, newTitle);
        notifyNoteChanged(id);
    }

    protected void ApplySetNoteTags(Guid id, IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors,
        IReadOnlyDictionary<string, Guid> newTagIds)
    {
        _board.SetNoteTags(id, tagNames, newTagColors, newTagIds);
        notifyNoteChanged(id);
    }

    protected void ApplyRenameTag(Guid tagId, string newName)
    {
        _board.RenameTag(tagId, newName);
        notifyTagChanged(tagId);
    }

    protected void ApplySetTagColor(Guid tagId, string color)
    {
        _board.SetTagColor(tagId, color);
        notifyTagChanged(tagId);
    }

    protected void ApplyDeleteTag(Guid tagId)
    {
        List<Guid> affectedColumnIds = _board.GetColumnIdsWithTag(tagId);
        _board.DeleteTag(tagId);
        foreach (Guid columnId in affectedColumnIds)
        {
            onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        }
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    protected void ApplySetNoteFileAttachment(Guid id, AttachedFile file)
    {
        _board.SetNoteFile(id, file);
        notifyNoteChanged(id);
    }

    protected void ApplyRemoveNoteFileAttachment(Guid id)
    {
        _board.SetNoteFile(id, null);
        notifyNoteChanged(id);
    }

    protected void ApplyMoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos)
    {
        bool sourceExists = fromColumnIndex >= 0 && fromColumnIndex < _board.StatusColumns.Count
                            && fromPos >= 0 && fromPos < _board.StatusColumns[fromColumnIndex].Notes.Count;
        if (!sourceExists)
        {
            return; // nothing to move, so nothing changes (ToDoBoard.MoveNote ignores it too)
        }

        Guid noteId = _board.StatusColumns[fromColumnIndex].Notes[fromPos].Id;
        moveNoteAndNotify(noteId, () => _board.MoveNote(fromColumnIndex, fromPos, toColumnIndex, toPos));
    }

    protected void ApplyMoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex)
    {
        moveNoteAndNotify(noteId, () => _board.MoveNoteToColumn(noteId, targetColumnId, targetIndex));
    }

    protected void ApplyMoveNoteDown(Guid noteId)  { moveNoteAndNotify(noteId, () => _board.MoveNoteDown(noteId)); }
    protected void ApplyMoveNoteUp(Guid noteId)    { moveNoteAndNotify(noteId, () => _board.MoveNoteUp(noteId)); }
    protected void ApplyMoveNoteRight(Guid noteId) { moveNoteAndNotify(noteId, () => _board.MoveNoteRight(noteId)); }
    protected void ApplyMoveNoteLeft(Guid noteId)  { moveNoteAndNotify(noteId, () => _board.MoveNoteLeft(noteId)); }

    protected void ApplyRemoveNote(Guid noteId)
    {
        Guid columnId = _board.GetColumnIdFromNoteId(noteId);
        _board.RemoveNote(noteId);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    protected void ApplyReplaceBoard(ToDoBoard loadedBoard)
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
