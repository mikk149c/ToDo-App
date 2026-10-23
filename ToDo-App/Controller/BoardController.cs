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

    // --- Changes ---

    public virtual void SetStatusColumnName(Guid columnId, string newName)
    {
        int columnIndex = _board.GetColumnIndexFromId(columnId);
        _board.StatusColumns[columnIndex].Title = newName;
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

    public void RemoveColumn(Guid columnId)
    {
        RemoveColumn(columnId, (Guid?)null);
    }

    public void RemoveColumn(Guid columnId, Guid moveNotesToColumnId)
    {
        RemoveColumn(columnId, (Guid?)moveNotesToColumnId);
    }

    /// <summary>Removes a column, deleting its notes, or moving them to <paramref name="moveNotesToColumnId"/> when given.</summary>
    protected virtual void RemoveColumn(Guid columnId, Guid? moveNotesToColumnId)
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

        _board.AddColumnNotesToTrashBin(columnId);
        _board.RemoveColumn(columnId);
        onColumnRemoved(new ColumnRemovedEventArgs(columnId));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public void MoveColumnLeft(Guid columnId)
    {
        throw new NotImplementedException();
    }

    public void MoveColumnRight(Guid columnId)
    {
        throw new NotImplementedException();
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

    public virtual void SetNoteContent(Guid id, string newContent)
    {
        _board.SetNoteContent(id, newContent);
        notifyNoteChanged(id);
    }

    public virtual void SetNoteTitle(Guid id, string newTitle)
    {
        _board.SetNoteTitle(id, newTitle);
        notifyNoteChanged(id);
    }

    public void SetNoteTags(Guid id, IEnumerable<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors = null)
    {
        List<string> names = tagNames.ToList();
        SetNoteTags(id, names, newTagColors, newTagIdsFor(names));
    }

    /// <param name="newTagIds">The Id for each tag name that isn't a tag on the board yet (tag name to Id).</param>
    protected virtual void SetNoteTags(Guid id, IReadOnlyList<string> tagNames, IReadOnlyDictionary<string, string>? newTagColors,
        IReadOnlyDictionary<string, Guid> newTagIds)
    {
        _board.SetNoteTags(id, tagNames, newTagColors, newTagIds);
        notifyNoteChanged(id);
    }

    public virtual void RenameTag(Guid tagId, string newName)
    {
        _board.RenameTag(tagId, newName);
        notifyTagChanged(tagId);
    }

    public virtual void SetTagColor(Guid tagId, string color)
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

    public void SetNoteFileAttachment(Guid id, string newFilePath)
    {
        // The name is read here, on the computer the file is on
        var fileInfo = new FileInfo(newFilePath);
        SetNoteFileAttachment(id, new AttachedFile(fileInfo.Name, newFilePath));
    }

    protected virtual void SetNoteFileAttachment(Guid id, AttachedFile file)
    {
        _board.SetNoteFile(id, file);
        notifyNoteChanged(id);
    }

    public virtual void RemoveNoteFileAttachment(Guid id)
    {
        _board.SetNoteFile(id, null);
        notifyNoteChanged(id);
    }

    public virtual void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos)
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

    public virtual void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex)
    {
        moveNoteAndNotify(noteId, () => _board.MoveNoteToColumn(noteId, targetColumnId, targetIndex));
    }

    public virtual void MoveNoteDown(Guid noteId)
    {
        moveNoteAndNotify(noteId, () => _board.MoveNoteDown(noteId));
    }

    public virtual void MoveNoteUp(Guid noteId)
    {
        moveNoteAndNotify(noteId, () => _board.MoveNoteUp(noteId));
    }

    public virtual void MoveNoteRight(Guid noteId)
    {
        moveNoteAndNotify(noteId, () => _board.MoveNoteRight(noteId));
    }

    public virtual void MoveNoteLeft(Guid noteId)
    {
        moveNoteAndNotify(noteId, () => _board.MoveNoteLeft(noteId));
    }

    public virtual void RemoveNote(Guid noteId)
    {
        Guid columnId = _board.GetColumnIdFromNoteId(noteId);
        _board.AddNoteToTrashBin(noteId);
        _board.RemoveNote(noteId);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public virtual void RemoveNoteFromTrashBin(Guid noteId)
    {
        _board.RemoveNoteFromTrashBin(noteId);
    }

    public virtual void RestoreNote(Guid noteId, Guid columnId)
    {
        _board.RestoreNoteFromTrashBin(noteId, columnId);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public virtual void EmptyTrashBin()
    {
        _board.EmptyTrashBin();
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
