namespace ToDo_App.Interfaces;

public class ColumnCreatedEventArgs(Guid columnId, string name, List<ToDoNote> notes) : EventArgs
{
    public Guid ColumnId = columnId;
    public string Name = name;
    public List<ToDoNote> Notes = notes;
}

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

public class BoardChangedEventArgs(ToDoBoard board) : EventArgs
{
    public ToDoBoard Board = board;
}