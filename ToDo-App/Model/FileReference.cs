namespace ToDo_App;

public class FileReference
{
    public enum FileReferenceType
    {
        Local,
        Uploaded
    }

    public FileReferenceType type { get; set; }
    public Guid guid { get; set; }
    public string name { get; set; }
    public string fullPath { get; set; }
    public Stream? stream { get; set; }
}