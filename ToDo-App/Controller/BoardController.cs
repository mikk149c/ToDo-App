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
        var names = new List<String>();
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
}