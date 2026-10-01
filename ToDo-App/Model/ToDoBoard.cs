namespace ToDo_App;

public class ToDoBoard
{
    public List<StatusColumn> StatusColumns { get; set; }

    public ToDoBoard()
    {
        StatusColumns = new List<StatusColumn>();
    }

    public void CreateColumn(string title)
    {
        var newColumn = new StatusColumn(title);
        StatusColumns.Add(newColumn);
    }

    public void RemoveColumn(int index)
    {
        StatusColumns.RemoveAt(index);
    }

    public void MoveColumn(int fromColumnIndex, int toColumnIndex)
    {
        if(fromColumnIndex == toColumnIndex)
        {
            return; //skip if it is the same index
        }

        StatusColumn column = StatusColumns[fromColumnIndex];
        StatusColumns.Insert(toColumnIndex, column);

        int deletePos = fromColumnIndex;
        if (toColumnIndex < fromColumnIndex)
        { 
            deletePos++; //handle change in the original column position, if the column is moved further left in the same list
        }
        StatusColumns.RemoveAt(deletePos);
    }

    public void SetColumnTitle(int columnIndex, string columnTitle)
    {
        StatusColumns[columnIndex].Title = columnTitle;
    }

    public (int columnIndex, int noteIndex) GetNotePositionFromId(Guid id)
    {
        for (int col = 0; col < StatusColumns.Count; col++)
        {
            for (int pos = 0; pos < StatusColumns[col].Notes.Count; pos++)
            {
                if (StatusColumns[col].Notes[pos].Id == id)
                {
                    return (col, pos);
                }
            }
        }

        return (-1,-1);
    }

    public void AddNote(int columnIndex, string title)
    {
        var note = new ToDoNote(title);
        StatusColumns[columnIndex].AddNote(note);
    }

    public void InsertNote(int columnIndex, int noteIndex, string title)
    {
        var note = new ToDoNote(title);
        StatusColumns[columnIndex].InsertNote(noteIndex, note);
    }

    public void RemoveNote(int columnIndex, int noteIndex)
    {
        StatusColumns[columnIndex].RemoveNote(noteIndex);
    }

    public void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos)
    {
        if(fromColumnIndex == toColumnIndex && fromPos == toPos)
        {
            return; //skip since it would land at the same position and column
        }

        ToDoNote note = StatusColumns[fromColumnIndex].Notes[fromPos];
        StatusColumns[toColumnIndex].Notes.Insert(toPos, note);

        int deletePos = fromPos;
        if (fromColumnIndex == toColumnIndex)
        {
            if (toPos < fromPos)
            { 
                deletePos++; //handle change in the original notes position, if the note is moved further up in the same list
            }
            
        }
        StatusColumns[fromColumnIndex].Notes.RemoveAt(deletePos);
    }

    public void MoveNoteDown(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        var startColumn = notePosTuple.columnIndex;
        var startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn, startPos + 1);
    }
    
    public void MoveNoteUp(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        var startColumn = notePosTuple.columnIndex;
        var startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn, startPos - 1);
    }

    public void SetNoteContent(int columnIndex, int noteIndex, string content)
    {
        StatusColumns[columnIndex].Notes[noteIndex].Content = content;
    }
}