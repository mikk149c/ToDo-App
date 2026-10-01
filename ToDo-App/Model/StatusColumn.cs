namespace ToDo_App;

public class StatusColumn
{
    public string Title { get; set; }
    public List<ToDoNote> Notes { get;}

    public StatusColumn(string title)
    {
        Title = title;
        Notes = new List<ToDoNote>();
    }

    public void AddNote(ToDoNote note)
    {
        Notes.Add(note);
    }

    public void InsertNote(int index, ToDoNote note)
    {
        //TO DO: handle list out of bounds
        Notes.Insert(index, note);
    }

    public void RemoveNote(int index)
    {
        Notes.RemoveAt(index);
    }
}