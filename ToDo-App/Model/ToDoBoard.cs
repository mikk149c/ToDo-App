namespace ToDo_App;

public class ToDoBoard
{
    public List<StatusColumn> StatusColumns { get; }

    public void CreateColumn(string title)
    {
        var newColumn = new StatusColumn(title);
        StatusColumns.Append(newColumn);
    }

    public void RemoveColumn(int index)
    {
        // TODO: handle invalid index
        StatusColumns.RemoveAt(index);
    }
}