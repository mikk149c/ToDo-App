namespace ToDo_App;

public class StatusColumn
{
    public string Title { get; set; }
    public List<ToDoNote> Notes { get; set; }

    public StatusColumn(string title)
    {
        Title = title;
        Notes = new List<ToDoNote>();
    }
}