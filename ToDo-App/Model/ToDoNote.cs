namespace ToDo_App;

public class ToDoNote
{
    public Guid Id { get; set; }
    public string Content { get; set; }

    public ToDoNote(String content)
    {
        this.Id = Guid.NewGuid();
        this.Content = content;
    }
}
