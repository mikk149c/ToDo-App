namespace ToDo_App.Interfaces;

public interface ITodoBoard
{
    public List<string> GetStatusColumnNames();

    public List<ToDoNote> GetNotesInColumn(string columnName);

    public void CreateColumn(string name);

    public void MoveColumnLeft(string name);

    public void MoveColumnRight(string right);

    public void CreateNote(string columnName, string content);

    public void SetNoteContent(Guid id, string newContent);
}