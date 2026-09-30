namespace ToDo_App;

public class ToDoBoard
{
    public List<StatusColumn> StatusColumns { get; }

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
        // TODO: handle invalid index
        StatusColumns.RemoveAt(index);
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
                deletePos++;
            }
            //handle change in position if note is deleted before or after insertion
        }
        StatusColumns[fromColumnIndex].Notes.RemoveAt(deletePos);
    }
}