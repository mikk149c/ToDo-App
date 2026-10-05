namespace ToDo_App;

public class StatusColumn
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public List<ToDoNote> Notes { get; set; }

    public StatusColumn(string title)
    {
        this.Id = Guid.NewGuid();
        Title = title;
        Notes = new List<ToDoNote>();
    }

    public void AddNote(ToDoNote note)
    {
        Notes.Add(note);
    }

    public void InsertNote(int index, ToDoNote note)
    {
        Notes.Insert(index, note);
    }

    public void RemoveNote(int index)
    {
        Notes.RemoveAt(index);
    }
}