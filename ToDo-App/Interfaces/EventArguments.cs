namespace ToDo_App.Interfaces;

public class ColumnNameChangedEventArgs(Guid columnId, string newName) : EventArgs
{
    public Guid ColumnId = columnId;
    public string NewName = newName;
}

public class ColumnContentChangedEventArgs(Guid columnId, List<ToDoNote> newNotes) : EventArgs
{
    public Guid ColumnId = columnId;
    public List<ToDoNote> NewNotes = newNotes;
}