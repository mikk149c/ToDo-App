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
    
    public List<String> GetStatusColumnNames()
    {
        var names = new List<string>();
        foreach (var column in _board.StatusColumns)
        {
            names.Add(column.Title);
        }

        return names;
    }

    public List<ToDoNote> GetNotesInColumn(string columnName)
    {
        //TODO: handle duplicate names
        
        foreach (var column in _board.StatusColumns)
        {
            if (column.Title == columnName)
            {
                return column.Notes;
            }
        }

        throw new Exception($"Column '{columnName}' does not exist");
    }

    public void CreateColumn(string name)
    {
        //TO DO block duplicate names at creation?
        _board.CreateColumn(name);
    }

    public void MoveColumnLeft(string name)
    {
        throw new NotImplementedException();
    }

    public void MoveColumnRight(string right)
    {
        throw new NotImplementedException();
    }

    int GetColumnIndexFromId(Guid columnId)
    {
        for (int i = 0; i < _board.StatusColumns.Count; i++)
        {
            if (_board.StatusColumns[i].Id == columnId)
                return i;
        }
        throw new Exception($"Column id '{columnId}' does not exist");
    }

    public void CreateNote(Guid columnId, string content)
    {
        _board.AddNote(columnId, new ToDoNote(content));
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