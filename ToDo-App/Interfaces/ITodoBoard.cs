namespace ToDo_App.Interfaces;

public interface ITodoBoard
{
    public List<(Guid, String)> GetStatusColumnIdsandNames();

    public List<ToDoNote> GetNotesInColumn(Guid columnId);

    public Guid CreateColumn(string name);

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
}