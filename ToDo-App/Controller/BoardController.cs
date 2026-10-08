using ToDo_App.Interfaces;

namespace ToDo_App.Controller;

public class BoardController : ITodoBoard
{
    private ToDoBoard _board;

    public BoardController()
    {
        //TODO: read from json
        _board = new ToDoBoard();
    }

    public event EventHandler? ColumnCreated;
    public event EventHandler? ColumnNameChanged;
    public event EventHandler? ColumnRemoved;
    public event EventHandler? ColumnContentChanged;
    public event EventHandler? BoardChanged;
    
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


    public void SetStatusColumnName(Guid columnId, string newName)
    {
        int columnIndex = _board.GetColumnIndexFromId(columnId);
        _board.StatusColumns[columnIndex].Title = newName;
        onColumnNameChanged(new ColumnNameChangedEventArgs(columnId, GetStatusColumnName(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }
    
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

    public Guid CreateColumn(string name)
    {
        var columnId = _board.CreateColumn(name);
        onColumnCreated(new ColumnCreatedEventArgs(columnId, GetStatusColumnName(columnId), GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
        return columnId;
    }

    public void RemoveColumn(Guid columnId)
    {
        _board.RemoveColumn(columnId);
        onColumnRemoved(new ColumnRemovedEventArgs(columnId));
    }

    public void RemoveColumn(Guid columnId, Guid moveNotesToColumnId)
    {
        if (columnId == moveNotesToColumnId)
        {
            throw new ArgumentException("Notes cannot be moved to the column that is being removed");
        }

        _board.MoveAllNotes(columnId, moveNotesToColumnId);
        onColumnContentChanged(new ColumnContentChangedEventArgs(moveNotesToColumnId, GetNotesInColumn(moveNotesToColumnId)));
        RemoveColumn(columnId);
    }

    public void MoveColumnLeft(Guid columnId)
    {
        throw new NotImplementedException();
    }

    public void MoveColumnRight(Guid columnId)
    {
        throw new NotImplementedException();
    }

    public Guid CreateNote(Guid columnId, string title, string content)
    {
        var noteId = _board.AddNote(columnId, new ToDoNote(title, content));
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
        return noteId;
    }

    ToDoNote GetNoteFromGuid(Guid id)
    {
        foreach (var column in _board.StatusColumns)
        {
            foreach (var note in column.Notes)
            {
                if (note.Id.Equals(id))
                    return note;
            }
        }

        throw new Exception($"Note not found (GUID {id})");
    }

    public void SetNoteContent(Guid id, string newContent)
    {
        //TODO handle exception
        _board.SetNoteContent(id, newContent);
        //TODO: remove column ID from arguments, as it can be derived from the note ID
        Guid columnId = _board.GetColumnIdFromNoteId(id);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public void SetNoteTitle(Guid id, string newTitle)
    {
        //TODO handle exception
        _board.SetNoteTitle(id, newTitle);
        Guid columnId = _board.GetColumnIdFromNoteId(id);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public void MoveNoteDown(Guid noteId)
    {
        //TODO handle exception
        _board.MoveNoteDown(noteId);
    }

    public void MoveNoteUp(Guid noteId)
    {
        //TODO handle exception
        _board.MoveNoteUp(noteId);
    }

    public void MoveNoteRight(Guid noteId)
    {
        //TODO handle exception
        _board.MoveNoteRight(noteId);
    }

    public void MoveNoteLeft(Guid noteId)
    {
        //TODO handle exception
        _board.MoveNoteLeft(noteId);
    }

    public void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos)
    {
        _board.MoveNote(fromColumnIndex, fromPos, toColumnIndex, toPos);
    }

    public void RemoveNote(Guid noteId)
    {
        //TODO handle exception
        Guid columnId = _board.GetColumnIdFromNoteId(noteId);
        _board.RemoveNote(noteId);
        onColumnContentChanged(new ColumnContentChangedEventArgs(columnId, GetNotesInColumn(columnId)));
        onBoardChanged(new BoardChangedEventArgs(_board));
    }

    public void SaveBoardToJson(string jsonPath)
    {
        var storeJson = new StoreJson<ToDoBoard>(jsonPath);
        storeJson.Save(_board);
    }

    public void LoadBoardFromJson(string jsonPath)
    {
        var storeJson = new StoreJson<ToDoBoard>(jsonPath);
        _board = storeJson.Load();
        foreach (var column in _board.StatusColumns)
        {
            onColumnCreated(new ColumnCreatedEventArgs(column.Id, column.Title, column.Notes));
        }
    }
}