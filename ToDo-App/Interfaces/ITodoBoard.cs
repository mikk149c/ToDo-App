namespace ToDo_App.Interfaces;

public interface ITodoBoard
{
    public event EventHandler ColumnCreated;
    public event EventHandler ColumnNameChanged;
    public event EventHandler ColumnRemoved;
    public event EventHandler ColumnContentChanged;
    
    public List<Guid> GetStatusColumnIds();

    public String GetStatusColumnName(Guid columnId);
    public void SetStatusColumnName(Guid columnId, string newName);

    public List<ToDoNote> GetNotesInColumn(Guid columnId);

    public Guid CreateColumn(string name);

    public void RemoveColumn(Guid columnId);

    public void RemoveColumn(Guid columnId, Guid moveNotesToColumnId);

    public void MoveColumnLeft(Guid columnId);

    public void MoveColumnRight(Guid columnId);

    public Guid CreateNote(Guid columnId, string title, string content);

    public void SetNoteContent(Guid id, string newContent);

    public void SetNoteTitle(Guid id, string newTitle);

    public void MoveNoteDown(Guid noteId);

    public void MoveNoteUp(Guid noteId);

    public void MoveNoteRight(Guid noteId);

    public void MoveNoteLeft(Guid noteId);

    public void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos);

    public void RemoveNote(Guid noteId);
}