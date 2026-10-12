namespace ToDo_App;

public class ToDoNote
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public AttachedFile? File { get; set; }

    public ToDoNote(String title, String content, AttachedFile? file = null)
    {
        this.Id = Guid.NewGuid();
        this.Title = title;
        this.Content = content;
        this.File = file;
    }
}
