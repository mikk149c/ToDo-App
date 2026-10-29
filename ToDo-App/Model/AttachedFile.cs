namespace ToDo_App;

public class AttachedFile
{
    public Guid Guid { get; set; }
    public string Name { get; set; }
    public string FullPath { get; set; }
    
    public AttachedFile(Guid guid, string name, string fullPath)
    {
        Guid = guid;
        Name = name;
        FullPath = fullPath;
    }
}