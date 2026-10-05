namespace ToDo_App.Interfaces;

public interface ITodoBoard
{
    public List<string> GetStatusColumnNames();

    public List<ToDoNote> GetNotesInColumn(string columnName);

    public void CreateColumn(string name);

    public void MoveColumnLeft(string name);

    public void MoveColumnRight(string right);

    public void CreateNote(Guid columnId, string content);

    public void SetNoteContent(Guid id, string newContent);

    public void MoveNoteDown(Guid noteId);

    public void MoveNoteUp(Guid noteId);

    public void MoveNoteRight(Guid noteId);

    public void MoveNoteLeft(Guid noteId);

    public void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos);
}