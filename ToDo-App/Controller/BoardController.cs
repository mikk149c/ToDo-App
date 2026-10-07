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

    public void SetStatusColumnName(Guid columnId, string newName)
    {
        int columnIndex = _board.GetColumnIndexFromId(columnId);
        _board.StatusColumns[columnIndex].Title = newName;
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
        return _board.CreateColumn(name);
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
        return _board.AddNote(columnId, new ToDoNote(title, content));
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
    }

    public void SetNoteTitle(Guid id, string newTitle)
    {
        //TODO handle exception
        _board.SetNoteTitle(id, newTitle);
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
}