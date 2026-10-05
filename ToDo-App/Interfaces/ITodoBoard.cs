namespace ToDo_App.Interfaces;

public interface ITodoBoard
{
    public List<String> GetStatusColumnNames();

    public List<ToDoNote> GetNotesInColumn(String columnName);
}