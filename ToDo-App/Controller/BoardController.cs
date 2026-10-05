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

    public void MoveColumnLeft(string name)
    {
        throw new NotImplementedException();
    }

    public void MoveColumnRight(string right)
    {
        throw new NotImplementedException();
    }

    public void CreateColumn(string name)
    {
        _board.CreateColumn(name);
    }

    int GetColumnIndexFromName(string columnName)
    {
        for (int i = 0; i < _board.StatusColumns.Count; i++)
        {
            if (_board.StatusColumns[i].Title == columnName)
                return i;
        }
        throw new Exception($"Column '{columnName}' does not exist");
    }

    public void CreateNote(string columnName, string content)
    {
        _board.AddNote(GetColumnIndexFromName(columnName), content);
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
        var note = GetNoteFromGuid(id);
        note.Content = newContent;
    }
}