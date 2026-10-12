namespace ToDo_App;

using System.Linq;

public class ToDoBoard
{
    public List<StatusColumn> StatusColumns { get; set; }

    public ToDoBoard()
    {
        StatusColumns = new List<StatusColumn>();
    }

    public Guid CreateColumn(string title)
    {
        var newColumn = new StatusColumn(title);
        StatusColumns.Add(newColumn);
        return newColumn.Id;
    }

    public void RemoveColumn(Guid id)
    {
        int index = GetColumnIndexFromId(id);
        StatusColumns.RemoveAt(index);
    }

    public void MoveAllNotes(Guid fromColumnId, Guid toColumnId)
    {
        int fromColumnIndex = GetColumnIndexFromId(fromColumnId);
        int toColumnIndex = GetColumnIndexFromId(toColumnId);
        if (fromColumnIndex == toColumnIndex)
        {
            return; //skip since the notes are already in the column
        }

        StatusColumns[toColumnIndex].Notes.AddRange(StatusColumns[fromColumnIndex].Notes);
        StatusColumns[fromColumnIndex].Notes.Clear();
    }

    public void MoveColumn(int fromColumnIndex, int toColumnIndex) //TO DO, discuss if the gui can handle specific column/note indexes
    {
        if (fromColumnIndex < 0 || fromColumnIndex >= StatusColumns.Count || toColumnIndex < 0 || toColumnIndex >= StatusColumns.Count ||fromColumnIndex == toColumnIndex)
        {
            return; //skip if it is going out of bounds or is the same index
        }

        StatusColumn column = StatusColumns[fromColumnIndex];
        StatusColumns.RemoveAt(fromColumnIndex);
        StatusColumns.Insert(toColumnIndex, column);
    }

    public void SetColumnTitle(Guid columnId, string columnTitle)
    {
        int columnIndex = GetColumnIndexFromId(columnId);
        StatusColumns[columnIndex].Title = columnTitle;
    }

    (int columnIndex, int noteIndex) GetNotePositionFromId(Guid id)
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

        throw new KeyNotFoundException($"Note id '{id}' does not exist");
    }

    internal int GetColumnIndexFromId(Guid columnId)
    {
        for (int i = 0; i < StatusColumns.Count; i++)
        {
            if (StatusColumns[i].Id == columnId)
                return i;
        }
        throw new KeyNotFoundException($"Column id '{columnId}' does not exist");
    }

    public Guid AddNote(Guid columnId, ToDoNote note)
    {
        int columnIndex = GetColumnIndexFromId(columnId);
        StatusColumns[columnIndex].AddNote(note);
        return note.Id;
    }

    public void RemoveNote(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        StatusColumns[columnIndex].RemoveNote(noteIndex);
    }

    public void MoveNote(int fromColumnIndex, int fromPos, int toColumnIndex, int toPos)
    {
        if (fromColumnIndex < 0 || fromColumnIndex >= StatusColumns.Count ||
        toColumnIndex < 0 || toColumnIndex >= StatusColumns.Count)
        {
            return; //skip since the position doesn't exist
        }
        List<ToDoNote> fromNotes = StatusColumns[fromColumnIndex].Notes;
        List<ToDoNote> toNotes = StatusColumns[toColumnIndex].Notes;

        if (fromPos < 0 || fromPos >= fromNotes.Count)
        {
            return; // skip as there is no note at source.
        }

        int maxPos = fromColumnIndex == toColumnIndex ? toNotes.Count - 1 : toNotes.Count;
        if (toPos < 0 || toPos > maxPos)
        {
            return; //skip since its trying to move beyond the last entry of the column
        }
        if (fromColumnIndex == toColumnIndex && fromPos == toPos)
        {
            return; // skip since the position is the same
        }
        ToDoNote note = fromNotes[fromPos];
        fromNotes.RemoveAt(fromPos);
        toNotes.Insert(toPos, note);

    }

    public void MoveNoteDown(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int startColumn = notePosTuple.columnIndex;
        int startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn, startPos + 2);
    }

    public void MoveNoteUp(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int startColumn = notePosTuple.columnIndex;
        int startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn, startPos - 1);
    }

    public void MoveNoteRight(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int startColumn = notePosTuple.columnIndex;
        int startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn + 1, 0);
    }

    public void MoveNoteLeft(Guid noteId)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int startColumn = notePosTuple.columnIndex;
        int startPos = notePosTuple.noteIndex;
        MoveNote(startColumn, startPos, startColumn - 1, 0);
    }

    public void SetNoteContent(Guid noteId, string content)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        StatusColumns[columnIndex].Notes[noteIndex].Content = content;
    }

    public void SetNoteFile(Guid noteId, AttachedFile? file)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        StatusColumns[columnIndex].Notes[noteIndex].File = file;
    }

    public void SetNoteTitle(Guid noteId, string title)
    {
        var notePosTuple = GetNotePositionFromId(noteId);
        int columnIndex = notePosTuple.columnIndex;
        int noteIndex = notePosTuple.noteIndex;
        StatusColumns[columnIndex].Notes[noteIndex].Title = title;
    }

    internal Guid GetColumnIdFromNoteId(Guid noteId)
    {
        Guid columnId = StatusColumns.Find(column => column.Notes.Exists(note => note.Id == noteId)).Id;
        return columnId;
    }

    public void MoveNoteToColumn(Guid noteId, Guid targetColumnId, int targetIndex)
    {
        var (fromColumnIndex, fromPos) = GetNotePositionFromId(noteId);
        int toColumnIndex = GetColumnIndexFromId(targetColumnId);
        MoveNote(fromColumnIndex, fromPos, toColumnIndex, targetIndex);
    }

    public void MoveColumn (Guid columnId, int targetIndex)
    {
        int fromIndex = GetColumnIndexFromId(columnId);
        MoveColumn(fromIndex, targetIndex);
    }
}